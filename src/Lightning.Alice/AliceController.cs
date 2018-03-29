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
		public Task<IActionResult> Fund(
			string bolt11,
			[ModelBinder(typeof(NodeInfoModelBinder))]
			NodeInfo node)
		{
			return null;
		}
	}
}
