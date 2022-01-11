using System.Collections.Generic;
using System.Linq;

namespace MiaCore.Infrastructure.Persistence
{
    public class DynamicQueryBuilder
    {
        private string _table;
        private string _select;
        private string _from;
        private string _where;
        private string _limit;
        private string _order;
        private bool _isCount;


        public DynamicQueryBuilder(string tableName)
        {
            _table = convertName(tableName);
            _select = $"SELECT {_table}.*";
            _from = $"from {_table}";
            _where = "";
            _limit = "";
            _order = "";
            _isCount = false;
        }

        public DynamicQueryBuilder WithOne(string tableName)
        {
            tableName = convertName(tableName);
            _select += $",{tableName}.*";
            _from += $" left join {tableName} on {tableName}.id = {_table}.{getColumnName(tableName)}";
            return this;
        }

        public DynamicQueryBuilder WithMany(string tableName)
        {
            tableName = convertName(tableName);
            _select += $",{tableName}.*";
            _from += $" left join {tableName} on {tableName}.{getColumnName(_table)} = {_table}.id";
            return this;
        }

        public DynamicQueryBuilder Where(List<Where> wheres)
        {
            if (wheres != null)
                foreach (var where in wheres)
                {
                    _where += !_where.StartsWith("where") ? "where" : " and";
                    if (!long.TryParse(where.Value, out _))
                        where.Value = $"\"{where.Value}\"";

                    _where += where.Type switch
                    {
                        WhereConditionType.Like => " concat(" + string.Join(",", where.Keys.Select(x => $"{_table}.{convertName(x)}")) + $") regexp {where.Value}",
                        _ => $" {_table}.{convertName(where.Key)} = {where.Value}"
                    };
                }
            return this;
        }

        public DynamicQueryBuilder OrderBy(List<Order> orders)
        {
            if (orders != null)
                foreach (var order in orders)
                {
                    _order += !_order.StartsWith("order by") ? "order by" : " ,";
                    _order += $" {order.Field} {order.Type}";
                }
            return this;
        }

        public DynamicQueryBuilder WithLimit(int? limit, int? page)
        {
            if (limit.HasValue)
                _limit = $"limit {((page ?? 0) <= 0 ? 0 : page - 1) * limit},{limit}";
            return this;
        }

        public DynamicQueryBuilder WithCount()
        {
            _isCount = true;
            return this;
        }

        public string Build()
        {
            var select = _isCount ? "select count(1) Count" : _select;
            var limit = _isCount ? "" : _limit;

            return $"{select} {_from} {_where} {_order} {limit}";
        }

        private string convertName(string name)
            => string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();

        private string getColumnName(string tableName)
        {
            tableName = convertName(tableName);
            tableName = tableName.StartsWith("mia_") ? tableName.Substring(4) : tableName;
            return tableName + "_id";
        }
    }
}