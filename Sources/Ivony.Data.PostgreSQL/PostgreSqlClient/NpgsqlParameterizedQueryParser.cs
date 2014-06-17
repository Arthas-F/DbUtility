using Ivony.Data.Queries;
using Npgsql;

namespace Ivony.Data.PostgreSQL.PostgreSqlClient
{
	/// <summary>
	/// ���� PostgreSQL ��������ѯ��������
	/// </summary>
	public class NpgsqlParameterizedQueryParser : ParameterizedQueryParser<NpgsqlCommand, NpgsqlParameter>
	{
		/// <summary>
		/// ������ʵ�ִ˷�������һ���������󣬲�����һ��ռλ���ַ�����
		/// </summary>
		/// <param name="value">����ֵ</param>
		/// <param name="index">��������λ��</param>
		/// <param name="parameter">��������</param>
		/// <returns>����ռλ��</returns>
		protected override string GetParameterPlaceholder(object value, int index, out NpgsqlParameter parameter)
		{
			var name = "@Param" + index;
			parameter = new NpgsqlParameter(name, value);

			return name;
		}

		/// <summary>
		/// �����������
		/// </summary>
		/// <param name="commandText">�����ı�</param>
		/// <param name="parameters">�������</param>
		/// <returns>�������</returns>
		protected override NpgsqlCommand CreateCommand(string commandText, NpgsqlParameter[] parameters)
		{
			var command = new NpgsqlCommand{
				CommandText = commandText
			};

			command.Parameters.AddRange(parameters);

			return command;
		}
	}
}