using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;


namespace testSQLConnection
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "{tablename:alpha}")] HttpRequest req, string tablename,
            ILogger log)
            
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? $"This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response. {tablename} is the tablename"
                : $"Hello, {name}. This HTTP triggered function executed successfully. {tablename} is the tablename";

            //con = connection string, set the connection string to the Synapse in an enviromental variable, and reference it using System.Environment.GetEnvironmentVarialble
            string con = System.Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
            //initiate list for building IDS
            List<string> ids = new List<string>();

            try
            {
                //create SQL Connection
                using (SqlConnection connection = new SqlConnection(con))
                {
                    //create SQL Command for gettting ids from the tablename that was given as input
                    string sql = $"select id from stg.{tablename}";

                    //responseMessage += $" {sql} ";   //for debugging purposes

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        //open connection and get list of ids in the staging table
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ids.Add(reader.GetString(0)); //add id to list
                            }

                        }
                    }

                    //concatinate id list into string
                    char delim = ',';
                    string idlist = String.Join(delim, ids);
                    
                    
                    //if list is not empty run next command
                    if (idlist.Length > 0)
                    {
                        // new command for update query
                        var sqlScript = $"update dbo.{tablename} set expirationdate = GETDATE() where id in ( {idlist} ) and expirationdate is NULL";
                        SqlCommand com = new SqlCommand(sqlScript, connection);

                        // PARAMS not Supported in Synapse,, maybe,, looking into it
                        //com.Parameters.Add("@TableName", System.Data.SqlDbType.Text);
                        //com.Parameters["@TableName"].Value = tablename;

                        //responseMessage += $"{tablename} inside sql,,, {idlist} ,, {sqlScript} inside sql"; //for debuggin purpose

                        //com.Parameters.Add("@idList", System.Data.SqlDbType.Text);
                        //com.Parameters["@idList"].Value = idlist;

                        com.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) //return bad juju if there is a sql error, need to build out message for updating pipeline
            {
                return new BadRequestObjectResult(new { message = $"SQL no good {ex}" });
            }

            //if everything is ok this message will populate on the screen, change or remove
            return new OkObjectResult(responseMessage);
        }       
    }
}
