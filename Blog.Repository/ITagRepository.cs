#region

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Blog.Entity;
using SmartSql.DyRepository;

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