using System;
using System.Collections.Generic;
using System.Text;

namespace Ivony.Data
{

  /// <summary>
  /// �������������
  /// </summary>
  public interface ITransactionUtility : IDisposable
  {

    /// <summary>
    /// ��ʼ����
    /// </summary>
    void Begin();

    /// <summary>
    /// �ύ����
    /// </summary>
    void Commit();

    /// <summary>
    /// �ع�����
    /// </summary>
    void Rollback();

    /// <summary>
    /// ��ȡ����ִ��SQL����DbUtilityʵ����
    /// </summary>
    DbUtility DbUtility
    {
      get;
    }
  }


  public interface ITransactionUtility<T> : ITransactionUtility where T : DbUtility
  {
    new T DbUtility
    {
      get;
    }
  }


}
