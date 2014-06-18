using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Ivony.Data.Common;
using Ivony.Data.Queries;
using Ivony.Fluent;
using Npgsql;

namespace Ivony.Data.PostgreSQL.PostgreSqlClient
{
    public class NpgsqlDbExecutor : DbExecutorBase, IAsyncDbExecutor<ParameterizedQuery>, IAsyncDbExecutor<StoredProcedureQuery>, IDbTransactionProvider<NpgsqlDbExecutor>
    {
        protected string ConnectionString { get; private set; }
        protected NpgsqlDbConfiguration Configuration { get; private set; }

        /// <summary>
        /// ��ʼ�� DbExecuterBase ����
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="configuration">��ǰҪʹ�õ����ݿ�����</param>
        public NpgsqlDbExecutor(string connectionString, NpgsqlDbConfiguration configuration) : base(configuration)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            if (configuration == null) throw new ArgumentNullException("configuration");

            this.ConnectionString = connectionString;
            this.Configuration = configuration;
        }

        protected virtual IDbExecuteContext Execute(NpgsqlCommand command, IDbTracing tracing)
        {
            try
            {
                TryExecuteTracing(tracing, t => t.OnExecuting(command));

                var connection = new NpgsqlConnection(this.ConnectionString);
                connection.Open();
                command.Connection = connection;

                if ( Configuration.QueryExecutingTimeout.HasValue )
                  command.CommandTimeout = (int) Configuration.QueryExecutingTimeout.Value.TotalSeconds;


                var reader = command.ExecuteReader();
                var context = new NpgsqlDbExecuteContext(connection, reader, tracing);

                TryExecuteTracing(tracing, t => t.OnLoadingData(context));

                return context;
            }
            catch (DbException exception)
            {
                TryExecuteTracing(tracing, t => t.OnException(exception));
                throw;
            }
        }

        /// <summary>
        /// �첽ִ�в�ѯ�������ִ��������
        /// </summary>
        /// <param name="command">��ѯ����</param>
        /// <param name="token">ȡ��ָʾ</param>
        /// <param name="tracing">����׷�ٲ�ѯ���̵�׷����</param>
        /// <returns>��ѯִ��������</returns>
        protected virtual async Task<IAsyncDbExecuteContext> ExecuteAsync(NpgsqlCommand command, CancellationToken token, IDbTracing tracing = null)
        {
            try
            {
                TryExecuteTracing(tracing, t => t.OnExecuting(command));

                var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync(token);
                command.Connection = connection;

                if ( Configuration.QueryExecutingTimeout.HasValue )
                  command.CommandTimeout = (int) Configuration.QueryExecutingTimeout.Value.TotalSeconds;


                var reader = await command.ExecuteReaderAsync(token);
                var context = new NpgsqlDbExecuteContext(connection, reader, tracing);

                TryExecuteTracing(tracing, t => t.OnLoadingData(context));

                return context;
            }
            catch (DbException exception)
            {
                TryExecuteTracing(tracing, t => t.OnException(exception));
                throw;
            }
        }

        protected NpgsqlCommand CreateCommand(ParameterizedQuery query)
        {
            return new NpgsqlParameterizedQueryParser().Parse(query);
        }

        /// <summary>
        /// ͨ���洢���̲�ѯ���� SqlCommand ����
        /// </summary>
        /// <param name="query">�洢���̲�ѯ����</param>
        /// <returns>SQL ��ѯ�������</returns>
        protected NpgsqlCommand CreateCommand(StoredProcedureQuery query)
        {
            var command = new NpgsqlCommand(query.Name){
                CommandType = CommandType.StoredProcedure
            };
            query.Parameters.ForAll(pair => command.Parameters.AddWithValue(pair.Key, pair.Value));

            return command;
        }

        #region Implementation of IDbExecutor<in ParameterizedQuery>

        /// <summary>
        /// ִ�в�ѯ
        /// </summary>
        /// <param name="query">��ѯ����</param>
        /// <returns>��ѯִ��������</returns>
        public IDbExecuteContext Execute(ParameterizedQuery query)
        {
            return this.Execute(this.CreateCommand(query), this.TryCreateTracing(this, query));
        }

        #endregion

        #region Implementation of IAsyncDbExecutor<ParameterizedQuery>

        /// <summary>
        /// �첽ִ�в�ѯ
        /// </summary>
        /// <param name="query">Ҫִ�еĲ�ѯ</param>
        /// <param name="token">ȡ��ָʾ</param>
        /// <returns>��ѯִ��������</returns>
        public Task<IAsyncDbExecuteContext> ExecuteAsync(ParameterizedQuery query, CancellationToken token)
        {
            return this.ExecuteAsync(this.CreateCommand(query), token, this.TryCreateTracing(this, query));
        }

        #endregion

        #region Implementation of IDbExecutor<in StoredProcedureQuery>

        /// <summary>
        /// ִ�в�ѯ
        /// </summary>
        /// <param name="query">��ѯ����</param>
        /// <returns>��ѯִ��������</returns>
        public IDbExecuteContext Execute(StoredProcedureQuery query)
        {
            return Execute(CreateCommand(query), TryCreateTracing(this, query));
        }

        #endregion

        #region Implementation of IAsyncDbExecutor<StoredProcedureQuery>

        /// <summary>
        /// �첽ִ�в�ѯ
        /// </summary>
        /// <param name="query">Ҫִ�еĲ�ѯ</param>
        /// <param name="token">ȡ��ָʾ</param>
        /// <returns>��ѯִ��������</returns>
        public Task<IAsyncDbExecuteContext> ExecuteAsync(StoredProcedureQuery query, CancellationToken token)
        {
            return ExecuteAsync(CreateCommand(query), token, TryCreateTracing(this, query));
        }

        #endregion

        #region Implementation of IDbTransactionProvider<out NpgsqlDbExecutor>

        /// <summary>
        /// ����һ�����ݿ�����������
        /// </summary>
        /// <returns>���ݿ�����������</returns>
        public IDbTransactionContext<NpgsqlDbExecutor> CreateTransaction()
        {
            return new NpgsqlDbTransactionContext(this.ConnectionString, this.Configuration);
        }

        #endregion
    }

    internal class NpgsqlDbExecutorWithTransaction : NpgsqlDbExecutor
    {
        public NpgsqlDbExecutorWithTransaction(NpgsqlDbTransactionContext transaction, NpgsqlDbConfiguration configuration)
            : base(transaction.Connection.ConnectionString, configuration)
        {
            TransactionContext = transaction;
        }


        /// <summary>
        /// ��ǰ����������
        /// </summary>
        protected NpgsqlDbTransactionContext TransactionContext { get; private set; }


        /// <summary>
        /// ��д ExecuteAsync ���������������첽ִ�в�ѯ
        /// </summary>
        /// <param name="command">Ҫִ�еĲ�ѯ����</param>
        /// <param name="token">ȡ��ָʾ</param>
        /// <param name="tracing">����׷�ٵ�׷����</param>
        /// <returns>��ѯִ��������</returns>
        protected sealed override async Task<IAsyncDbExecuteContext> ExecuteAsync(NpgsqlCommand command, CancellationToken token, IDbTracing tracing = null)
        {
            try
            {
                TryExecuteTracing(tracing, t => t.OnExecuting(command));

                command.Connection = TransactionContext.Connection;
                command.Transaction = TransactionContext.Transaction;

                var reader = await command.ExecuteReaderAsync(token);
                var context = new NpgsqlDbExecuteContext(TransactionContext, reader, tracing);

                TryExecuteTracing(tracing, t => t.OnLoadingData(context));

                return context;
            }
            catch (DbException exception)
            {
                TryExecuteTracing(tracing, t => t.OnException(exception));
                throw;
            }

        }


        /// <summary>
        /// ִ�в�ѯ�������ִ��������
        /// </summary>
        /// <param name="command">��ѯ����</param>
        /// <param name="tracing">����׷�ٲ�ѯ���̵�׷����</param>
        /// <returns>��ѯִ��������</returns>
        protected sealed override IDbExecuteContext Execute(NpgsqlCommand command, IDbTracing tracing = null)
        {
            try
            {
                TryExecuteTracing(tracing, t => t.OnExecuting(command));

                command.Connection = TransactionContext.Connection;
                command.Transaction = TransactionContext.Transaction;

                var reader = command.ExecuteReader();
                var context = new NpgsqlDbExecuteContext(TransactionContext, reader, tracing);

                TryExecuteTracing(tracing, t => t.OnLoadingData(context));

                return context;
            }
            catch (DbException exception)
            {
                TryExecuteTracing(tracing, t => t.OnException(exception));
                throw;
            }
        }
    }
}