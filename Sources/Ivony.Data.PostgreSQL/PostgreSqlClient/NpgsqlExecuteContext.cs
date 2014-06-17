using System.Data.Common;
using Ivony.Data.Common;
using Npgsql;

namespace Ivony.Data.PostgreSQL.PostgreSqlClient
{
	public class NpgsqlDbExecuteContext : AsyncDbExecuteContextBase
	{
		/// <summary>
		/// ���� NpgsqlExecuteContext ����
		/// </summary>
		/// <param name="connection">PostgreSql ���ݿ�����</param>
		/// <param name="dataReader">PostgreSql ���ݶ�ȡ��</param>
		/// <param name="tracing">���ڵ�ǰ��ѯ��׷����</param>
		public NpgsqlDbExecuteContext(NpgsqlConnection connection, DbDataReader dataReader, IDbTracing tracing)
			: base( dataReader, connection, tracing )
		{
			this.NpgsqlDataReader = dataReader;
		}

		/// <summary>
		/// ���� NpgsqlExecuteContext ����
		/// </summary>
		/// <param name="transaction">PostgreSql ���ݿ�����������</param>
		/// <param name="dataReader">PostgreSql ���ݶ�ȡ��</param>
		/// <param name="tracing">���ڵ�ǰ��ѯ��׷����</param>
		public NpgsqlDbExecuteContext(NpgsqlDbTransactionContext transaction, DbDataReader dataReader, IDbTracing tracing)
			: base(dataReader, null, tracing)
		{
			this.TransactionContext = transaction;
			this.NpgsqlDataReader = dataReader;
		}

		public DbDataReader NpgsqlDataReader { get; private set; }

		/// <summary>
		/// ���ݿ����������ģ�����еĻ�
		/// </summary>
		public NpgsqlDbTransactionContext TransactionContext { get; private set; }
	}
}