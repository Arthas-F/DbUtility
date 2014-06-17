using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Ivony.Data.Common;
using Npgsql;

namespace Ivony.Data.PostgreSQL.PostgreSqlClient
{
	/// <summary>
	/// PostgreSql ���ݿ����������Ķ���
	/// </summary>
	public class NpgsqlDbTransactionContext : DbTransactionContextBase<NpgsqlDbExecutor, NpgsqlTransaction> 
	{
		private readonly NpgsqlDbExecutor _executor;

		internal NpgsqlDbTransactionContext(string connectionString, NpgsqlDbConfiguration configuration)
		{
			this.Connection = new NpgsqlConnection(connectionString);
			this._executor = new NpgsqlDbExecutorWithTransaction(this, configuration);
		}

		#region Overrides of DbTransactionContextBase<NpgsqlDbExecutor,NpgsqlTransaction>

		/// <summary>
		/// ������ʵ�ִ˷����Դ������ݿ��������
		/// </summary>
		/// <returns>���ݿ��������</returns>
		protected override NpgsqlTransaction CreateTransaction()
		{
			if (this.Connection.State == ConnectionState.Closed)
			{
				this.Connection.Open();
			}

			return this.Connection.BeginTransaction();
		}

		/// <summary>
		/// ��ȡ��������ִ�в�ѯ��ִ����
		/// </summary>
		public override NpgsqlDbExecutor DbExecutor
		{
			get { return this._executor; }
		}

		#endregion

		public NpgsqlConnection Connection { get; private set; }

		internal class NpgsqlDbExecutorWithTransaction : NpgsqlDbExecutor 
		{
			public NpgsqlDbExecutorWithTransaction(NpgsqlDbTransactionContext transaction, NpgsqlDbConfiguration configuration)
				: base( transaction.Connection.ConnectionString, configuration )
			{
				this.TransactionContext = transaction;
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
}