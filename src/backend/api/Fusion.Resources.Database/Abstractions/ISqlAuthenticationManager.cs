using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fusion.Resources.Database
{
    public interface ISqlAuthenticationManager
    {
        SqlConnection GetSqlConnection();
    }
}
