#region

using System;

#endregion

namespace Blog.Entity
{
    /// <summary>
    /// Users
    /// </summary>
    public class Users
    {
        /// <summary>
        /// CreatedAt
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        public bool IsDeleted { get; set; }

        /// <summary>
        /// LoginName
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// RealName
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// Remark
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// UpdatedAt
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// �ϴ�ƾ�ݱ��ʱ��
        /// 
        /// ����ʱ�����ƾ�� Cookie �е� AuthUpdatedAt ֵʱ����Ҫ��� Cookie ���û����µ�¼
        /// </summary>
        public DateTime AuthUpdatedAt { get; set; }
    }
}