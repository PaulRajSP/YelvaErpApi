using System.Data;
using Microsoft.Data.SqlClient;

namespace HoneyErpAPI.Common
{
    public class DbClient
    {
        private readonly string _connectionString;

        public DbClient(IConfiguration configuration)
        {
            _connectionString = configuration["DBConnectionStrings:DefaultConnection"]
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
        }

        // ----------------------------------------------------------------
        // Returns a filled DataSet � use for SELECT queries
        // ----------------------------------------------------------------

        public DataSet ExecuteAdapter(string commandText, SqlParameter[]? parameters, CommandType commandType)
        {
            if (string.IsNullOrEmpty(commandText))
                throw new Exception("CommandText cannot be empty");

            DataSet dataSet = new DataSet();

            using SqlConnection sqlConnection = new SqlConnection(_connectionString);
            SqlCommand sqlCommand = new SqlCommand
            {
                CommandText = commandText,
                Connection = sqlConnection,
                CommandType = commandType
            };

            try
            {
                if (parameters != null && parameters.Length != 0)
                {
                    foreach (SqlParameter parameter in parameters)
                    {
                        SqlParameter newParameter = new SqlParameter
                        {
                            ParameterName = parameter.ParameterName,
                            SqlDbType = parameter.SqlDbType,
                            Direction = parameter.Direction,
                            Size = parameter.Size,
                            Precision = parameter.Precision,
                            Scale = parameter.Scale,
                            IsNullable = parameter.IsNullable,
                            Value = parameter.Value
                        };

                        sqlCommand.Parameters.Add(newParameter);
                    }
                }

                sqlConnection.Open();
                new SqlDataAdapter(sqlCommand).Fill(dataSet);
            }
            catch (SqlException ex)
            {
                ThrowErrMsgForException(ex);
                throw;
            }
            finally
            {
                sqlConnection.Close();
                sqlCommand.Dispose();
            }

            return dataSet;
        }


        // ----------------------------------------------------------------
        // Returns rows affected � use for INSERT / UPDATE / DELETE
        // ----------------------------------------------------------------
        public int ExecuteNonQuery(string commandText, SqlParameter[]? parameters, CommandType commandType)
        {
            if (string.IsNullOrEmpty(commandText))
                throw new Exception("CommandText cannot be empty");

            int result = -1;

            using SqlConnection sqlConnection = new SqlConnection(_connectionString);
            SqlCommand sqlCommand = new SqlCommand
            {
                CommandText = commandText,
                Connection  = sqlConnection,
                CommandType = commandType
            };

            try
            {
                if (parameters != null && parameters.Length != 0)
                    sqlCommand.Parameters.AddRange(parameters);

                sqlConnection.Open();
                result = sqlCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                ThrowErrMsgForException(ex);
                throw;
            }
            finally
            {
                sqlConnection.Close();
                sqlCommand.Dispose();
            }

            return result;
        }

        // ----------------------------------------------------------------
        // Returns a single scalar value
        // ----------------------------------------------------------------
        public T ExecuteScalar<T>(string commandText, SqlParameter[]? parameters, CommandType commandType)
        {
            if (string.IsNullOrEmpty(commandText))
                throw new Exception("CommandText cannot be empty");

            using SqlConnection sqlConnection = new SqlConnection(_connectionString);
            using SqlCommand sqlCommand       = new SqlCommand(commandText, sqlConnection);

            sqlCommand.CommandType = commandType;

            if (parameters != null && parameters.Length > 0)
                sqlCommand.Parameters.AddRange(parameters);

            try
            {
                sqlConnection.Open();
                object result = sqlCommand.ExecuteScalar()!;
                if (result == null || result == DBNull.Value) return default!;
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (SqlException ex)
            {
                ThrowErrMsgForException(ex);
                throw;
            }
        }

        // ----------------------------------------------------------------
        private static void ThrowErrMsgForException(SqlException ex)
        {
            if (ex.Number == 8115)
                throw new Exception("The number exceeds the allowed limit. " + ex.Message);
        }
    }
}
