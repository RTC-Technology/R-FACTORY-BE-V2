using MySqlConnector;
using RepoDb;
using System.Data;
using System.Linq.Expressions;

namespace R_FACTORY_BE.Repositories;

public interface IGenericRepo
{
    Task<List<T>> GetAll<T>() where T : class;
    Task<T?> GetById<T>(object id) where T : class;

    Task<T> Insert<T>(T entity) where T : class;
    Task<T> Update<T>(T entity) where T : class;

    Task<bool> Delete<T>(T entity) where T : class;
    Task<bool> DeleteById<T>(object id) where T : class;
    Task<bool> DeleteByExpression<T>(Expression<Func<T, bool>> predicate) where T : class;

    Task<List<T>> FindByExpression<T>(Expression<Func<T, bool>> predicate) where T : class;
    Task<T?> FindModel<T>(Expression<Func<T, bool>> predicate) where T : class;

    Task<List<T>> ProcedureToList<T>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues) where T : class;

    Task<Tuple<List<T1>, List<T2>>> ProcedureToList<T1, T2>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues)
        where T1 : class
        where T2 : class;

    Task<Tuple<List<T1>, List<T2>, List<T3>>> ProcedureToList<T1, T2, T3>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues)
        where T1 : class
        where T2 : class
        where T3 : class;

    Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ProcedureToList<T1, T2, T3, T4>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class;

    Task<int> ExecuteProcedureAsync(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues);
}

public class GenericRepo(string connectionString) : IGenericRepo
{
    private MySqlConnection CreateConnection()
        => new(connectionString);

    public async Task<List<T>> GetAll<T>() where T : class
    {
        using var conn = CreateConnection();
        var rows = await conn.QueryAllAsync<T>();
        return rows.ToList();
    }

    public async Task<T?> GetById<T>(object id) where T : class
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<T>(id)).FirstOrDefault();
    }

    public async Task<T> Insert<T>(T entity) where T : class
    {
        using var conn = CreateConnection();
        await conn.InsertAsync(entity);
        return entity;
    }

    public async Task<T> Update<T>(T entity) where T : class
    {
        using var conn = CreateConnection();
        await conn.UpdateAsync(entity);
        return entity;
    }

    public async Task<bool> Delete<T>(T entity) where T : class
    {
        using var conn = CreateConnection();
        var affected = await conn.DeleteAsync(entity);
        return affected > 0;
    }

    public async Task<bool> DeleteById<T>(object id) where T : class
    {
        using var conn = CreateConnection();
        var affected = await conn.DeleteAsync<T>(id);
        return affected > 0;
    }

    public async Task<bool> DeleteByExpression<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        using var conn = CreateConnection();
        var affected = await conn.DeleteAsync(predicate);
        return affected > 0;
    }

    public async Task<List<T>> FindByExpression<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        using var conn = CreateConnection();
        var rows = await conn.QueryAsync(predicate);
        return rows.ToList();
    }

    public async Task<T?> FindModel<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync(predicate)).FirstOrDefault();
    }

    public async Task<List<T>> ProcedureToList<T>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues) where T : class
    {
        using var conn = CreateConnection();
        var parameters = GetParameters(parameterNames, parameterValues);

        // MySQL procedure call as raw SQL
        var sql = BuildCallText(procedureName, parameterNames);

        var rows = await conn.ExecuteQueryAsync<T>(sql, parameters);
        return rows.ToList();
    }

    public Task<Tuple<List<T1>, List<T2>>> ProcedureToList<T1, T2>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues)
        where T1 : class
        where T2 : class
    {
        using var conn = CreateConnection();
        var parameters = GetParameters(parameterNames, parameterValues);
        var sql = BuildCallText(procedureName, parameterNames);

        using var result = conn.ExecuteQueryMultiple(sql, parameters);

        var list1 = result.Extract<T1>().ToList();
        var list2 = result.Extract<T2>().ToList();

        return Task.FromResult(Tuple.Create(list1, list2));
    }

    public Task<Tuple<List<T1>, List<T2>, List<T3>>> ProcedureToList<T1, T2, T3>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        using var conn = CreateConnection();
        var parameters = GetParameters(parameterNames, parameterValues);
        var sql = BuildCallText(procedureName, parameterNames);

        using var result = conn.ExecuteQueryMultiple(sql, parameters);

        var list1 = result.Extract<T1>().ToList();
        var list2 = result.Extract<T2>().ToList();
        var list3 = result.Extract<T3>().ToList();

        return Task.FromResult(Tuple.Create(list1, list2, list3));
    }

    public Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ProcedureToList<T1, T2, T3, T4>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
    {
        using var conn = CreateConnection();
        var parameters = GetParameters(parameterNames, parameterValues);
        var sql = BuildCallText(procedureName, parameterNames);

        using var result = conn.ExecuteQueryMultiple(sql, parameters);

        var list1 = result.Extract<T1>().ToList();
        var list2 = result.Extract<T2>().ToList();
        var list3 = result.Extract<T3>().ToList();
        var list4 = result.Extract<T4>().ToList();

        return Task.FromResult(Tuple.Create(list1, list2, list3, list4));
    }

    public async Task<int> ExecuteProcedureAsync(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues)
    {
        using var conn = CreateConnection();
        var parameters = GetParameters(parameterNames, parameterValues);
        var sql = BuildCallText(procedureName, parameterNames);

        return await conn.ExecuteNonQueryAsync(sql, parameters);
    }

    private static Dictionary<string, object?> GetParameters(
        string[] parameterNames,
        object[] parameterValues)
    {
        if (parameterNames.Length != parameterValues.Length)
        {
            throw new ArgumentException("The number of parameter names and values must be the same.");
        }

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < parameterNames.Length; i++)
        {
            var name = parameterNames[i].StartsWith('@')
                ? parameterNames[i].TrimStart('@')
                : parameterNames[i];

            parameters[name] = parameterValues[i] ?? DBNull.Value;
        }

        return parameters;
    }

    private static string BuildCallText(string procedureName, string[] parameterNames)
    {
        var args = parameterNames
            .Select(x => x.StartsWith('@') ? x : '@' + x);

        return $"CALL {procedureName}({string.Join(", ", args)});";
    }
}