#region

using Blog.Entity;
using SmartSql.DyRepository;
using System.Threading.Tasks;

#endregion

namespace Blog.Repository
{
    public interface ITagRepository : IRepositoryAsync<Tag, int>
    {
        Task SetArticleTagsAsync(int articleId, string[] tags);
    }
}

//---
//title: ���Ǳ���
//date: 2018-12-03 00:00
//categories:
//- ����1
//- ����2
//tags:
//- ��ǩ1
//- ��ǩ2
//---

//��������