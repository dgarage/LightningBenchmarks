using Common.CLightning;
using Common.ModelBinders;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lightning.Alice
{
    public class AliceController : Controller
    {
		[HttpPost]
		public Task<IActionResult> Fund(
			[ModelBinder(typeof(NodeInfoModelBinder))]
			NodeInfo nodeInfo)
		{
			return null;
		}
	}
}
