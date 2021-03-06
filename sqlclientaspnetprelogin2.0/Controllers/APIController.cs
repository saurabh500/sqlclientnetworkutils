﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using sqlclientaspnetprelogin.Models;
using sqlprelogin;
using Microsoft.Extensions.Configuration;

namespace sqlclientaspnetprelogin.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private IConfiguration _configuration;
        private string _host;
        private int _port;

        public ValuesController(IConfiguration configuration)
        {
            this._configuration = configuration;
            this._host = _configuration["Host"];
            int.TryParse(_configuration["Port"], out _port);
        }


        [HttpGet]
        public long Index()
        {
            long totalTimeTaken = 0;
            for(int i = 0; i < 30; i++) { 
                SQLConnection connection = new SQLConnection(this._host, this._port);
                totalTimeTaken += connection.Connect();
            }
            return totalTimeTaken;
        }
    }
}
