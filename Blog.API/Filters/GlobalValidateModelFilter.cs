#region

using Blog.API.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.Text;

#endregion

namespace Blog.API.Filters
{
    public class GlobalValidateModelFilter : IActionFilter
    {
        private readonly IHostingEnvironment _env;
        private readonly ILogger<GlobalValidateModelFilter> _logger;

        public GlobalValidateModelFilter(IHostingEnvironment env, ILogger<GlobalValidateModelFilter> logger)
        {
            _env = env;
            _logger = logger;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                _logger.LogDebug("ModelState not valid:{0}", JsonConvert.SerializeObject(context.ModelState));
                var errStr = new StringBuilder();

                foreach (var error in context.ModelState.Values)
                {
                    var errorMsg = error.Errors?.FirstOrDefault()?.ErrorMessage;
                    errStr.AppendFormat("{0} |", errorMsg);
                }

                var resp = new ResponseMessage<object>
                {
                    ErrorCode = "0006",
                    Message = errStr.ToString().TrimEnd('|'),
                    Succeed = false
                };

                var result = new JsonResult(resp);
                context.Result = result;
            }
        }
    }
}