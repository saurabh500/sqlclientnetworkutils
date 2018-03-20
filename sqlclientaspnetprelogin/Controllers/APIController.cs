using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using sqlclientaspnetprelogin.Models;
using sqlprelogin;

namespace sqlclientaspnetprelogin.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        [HttpGet]
        public long Index()
        {
            SQLConnection connection = new SQLConnection("ss-desktop2", 1433);
            long timeTaken = connection.Connect();
            return timeTaken;
        }
    }
}
