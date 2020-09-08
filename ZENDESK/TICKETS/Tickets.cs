#region Help:  Introduction to the Script Component

/* The Script Component allows you to perform virtually any operation that can be accomplished in

* a .Net application within the context of an Integration Services data flow.

*

* Expand the other regions which have "Help" prefixes for examples of specific ways to use

* Integration Services features within this script component. */

#endregion



#region Namespaces

using System;

using System.Data;

using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

using Microsoft.SqlServer.Dts.Runtime.Wrapper;

using Microsoft.CSharp;

using RestSharp;

using System.IO;

using System.Windows.Forms;

using System.Net;

using Newtonsoft.Json;

using System.Collections.Generic;

using RestSharp.Authenticators;

using Newtonsoft.Json.Linq;

#endregion


[Microsoft.SqlServer.Dts.Pipeline.SSISScriptComponentEntryPointAttribute]

public class ScriptMain : UserComponent

{

    #region Help:  Using Integration Services variables and parameters

    /* To use a variable in this script, first ensure that the variable has been added to

     * either the list contained in the ReadOnlyVariables property or the list contained in

     * the ReadWriteVariables property of this script component, according to whether or not your

     * code needs to write into the variable.  To do so, save this script, close this instance of

     * Visual Studio, and update the ReadOnlyVariables and ReadWriteVariables properties in the

     * Script Transformation Editor window.

     * To use a parameter in this script, follow the same steps. Parameters are always read-only.

     *

     * Example of reading from a variable or parameter:

     *  DateTime startTime = Variables.MyStartTime;

     *

     * Example of writing to a variable:

     *  Variables.myStringVariable = "new value";

     */

    #endregion



    #region Help:  Using Integration Services Connnection Managers

    /* Some types of connection managers can be used in this script component.  See the help topic

     * "Working with Connection Managers Programatically" for details.

     *

     * To use a connection manager in this script, first ensure that the connection manager has

     * been added to either the list of connection managers on the Connection Managers page of the

     * script component editor.  To add the connection manager, save this script, close this instance of

     * Visual Studio, and add the Connection Manager to the list.

     *

     * If the component needs to hold a connection open while processing rows, override the

     * AcquireConnections and ReleaseConnections methods.

     *

     * Example of using an ADO.Net connection manager to acquire a SqlConnection:

     *  object rawConnection = Connections.SalesDB.AcquireConnection(transaction);

     *  SqlConnection salesDBConn = (SqlConnection)rawConnection;

     *

     * Example of using a File connection manager to acquire a file path:

     *  object rawConnection = Connections.Prices_zip.AcquireConnection(transaction);

     *  string filePath = (string)rawConnection;

     *

     * Example of releasing a connection manager:

     *  Connections.SalesDB.ReleaseConnection(rawConnection);

     */

    #endregion



    #region Help:  Firing Integration Services Events

    /* This script component can fire events.

     *

     * Example of firing an error event:

     *  ComponentMetaData.FireError(10, "Process Values", "Bad value", "", 0, out cancel);

     *

     * Example of firing an information event:

     *  ComponentMetaData.FireInformation(10, "Process Values", "Processing has started", "", 0, fireAgain);

     *

     * Example of firing a warning event:

     *  ComponentMetaData.FireWarning(10, "Process Values", "No rows were received", "", 0);

     */

    #endregion

    Int64 EndTime = 0;
    public class CustomFields
    {
        public Int64 id { get; set; }
        public string value { get; set; }
    }


    public class Detalhes
    {
        public string url { get; set; }
        public Int64 id { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string type { get; set; }
        public string subject { get; set; }
        public string raw_subject { get; set; }
        public string description { get; set; }
        public string priority { get; set; }
        public string status { get; set; }
        public string recipient { get; set; }
        public Int64 requester_id { get; set; }
        public Int64 submitter_id { get; set; }
        public Int64 assignee_id { get; set; }
        public Int64 organization_id { get; set; }
        public Int64 group_id { get; set; }
        public string has_incidents { get; set; }
        public Int64 ticket_form_id { get; set; }
        public List<CustomFields> custom_fields { get; set; }
        public string[] collaborator_ids { get; set; }
        public string[] follower_ids { get; set; }
        public string[] tags { get; set; }
        public string[] email_cc_ids { get; set; }

    }

    public class Tickets
    {
        public Detalhes[] tickets { get; set; }
        public bool end_of_stream { get; set; }
        public Int64 end_time { get; set; }
    }

    public override void PreExecute()

    {
        base.PreExecute();
    }



    /// <summary>

    /// This method is called after all the rows have passed through this component.

    ///

    /// You can delete this method if you don't need to do anything here.

    /// </summary>

    public override void PostExecute()

    {

        base.PostExecute();

        this.ReadWriteVariables["User::EndTime"].Value = EndTime;

    }

    public override void CreateNewOutputRows()

    {
        ServicePointManager.Expect100Continue = true;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

       
        string Url = Variables.ZendeskUrl;
        string User = Variables.ZendeskUser;
        string Pass = Variables.ZendeskPass;
        Int64 StartTime = Variables.FuncTime;
        string Call = Url + "incremental/tickets.json?per_page=1000&start_time=" + StartTime.ToString();
        
        var client = new RestClient(Call);

        client.Timeout = -1;
        client.Authenticator = new HttpBasicAuthenticator(User, Pass);
        
        var request = new RestRequest(Method.GET);
        IRestResponse response = client.Execute(request);

        if (response.IsSuccessful)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var APIDetalhes = JsonConvert.DeserializeObject<Tickets>(response.Content, settings);
            if(APIDetalhes.end_of_stream)
            {
                EndTime = Variables.DataFimUnix;
            }
            else
            {
                EndTime = APIDetalhes.end_time;
            }

            //MessageBox.Show(Variables.DataFimUnix.ToString() + " - " + EndTime.ToString());
            
            foreach (var Det in APIDetalhes.tickets)
            {
                if (Det.custom_fields.Count > 0)
                {
                    foreach (var cf in Det.custom_fields)
                    {
                        if (cf.value != "null" && cf.value != "NULL" && cf.value != null)
                        {
                            OutputAPIBuffer.AddRow();
                            OutputAPIBuffer.url = Det.url;
                            OutputAPIBuffer.id = Det.id;
                            OutputAPIBuffer.createdat = Det.created_at.ToLocalTime();
                            OutputAPIBuffer.updatedat = Det.updated_at.ToLocalTime();
                            OutputAPIBuffer.type = Det.type;
                            OutputAPIBuffer.subject = Det.subject;
                            OutputAPIBuffer.rawsubject = Det.raw_subject;
                            if (Det.description.Length >= 8000)
                            {
                                OutputAPIBuffer.description = Det.description.Substring(0, 8000);
                            }
                            else
                            {
                                OutputAPIBuffer.description = Det.description;
                            }
                            OutputAPIBuffer.priority = Det.priority;
                            OutputAPIBuffer.status = Det.status;
                            OutputAPIBuffer.recipient = Det.recipient;
                            OutputAPIBuffer.requesterid = Det.requester_id;
                            OutputAPIBuffer.submitterid = Det.submitter_id;
                            OutputAPIBuffer.assigneeid = Det.assignee_id;
                            OutputAPIBuffer.organizationid = Det.organization_id;
                            OutputAPIBuffer.groupid = Det.group_id;
                            OutputAPIBuffer.hasincidents = Det.has_incidents;
                            OutputAPIBuffer.customfieldid = cf.id;
                            OutputAPIBuffer.customfieldvalue = cf.value;
                            OutputAPIBuffer.ticketformid = Det.ticket_form_id;
                            OutputAPIBuffer.tags = string.Join(",", Det.tags);
                            OutputAPIBuffer.collaboratorids = string.Join(",", Det.collaborator_ids);
                            OutputAPIBuffer.followerids = string.Join(",", Det.follower_ids);
                            OutputAPIBuffer.emailccids = string.Join(",", Det.email_cc_ids);
                        }


                    }
                }
                else
                {
                    OutputAPIBuffer.AddRow();
                    OutputAPIBuffer.url = Det.url;
                    OutputAPIBuffer.id = Det.id;
                    OutputAPIBuffer.createdat = Det.created_at.ToLocalTime();
                    OutputAPIBuffer.updatedat = Det.updated_at.ToLocalTime();
                    OutputAPIBuffer.type = Det.type;
                    OutputAPIBuffer.subject = Det.subject;
                    OutputAPIBuffer.rawsubject = Det.raw_subject;
                    if (Det.description.Length >= 8000)
                    {
                        OutputAPIBuffer.description = Det.description.Substring(0, 8000);
                    }
                    else
                    {
                        OutputAPIBuffer.description = Det.description;
                    }
                    OutputAPIBuffer.priority = Det.priority;
                    OutputAPIBuffer.status = Det.status;
                    OutputAPIBuffer.recipient = Det.recipient;
                    OutputAPIBuffer.requesterid = Det.requester_id;
                    OutputAPIBuffer.submitterid = Det.submitter_id;
                    OutputAPIBuffer.assigneeid = Det.assignee_id;
                    OutputAPIBuffer.organizationid = Det.organization_id;
                    OutputAPIBuffer.groupid = Det.group_id;
                    OutputAPIBuffer.hasincidents = Det.has_incidents;
                    OutputAPIBuffer.ticketformid = Det.ticket_form_id;
                }



            }
        }
        else
        {
            OutputErroAPIBuffer.AddRow();
            OutputErroAPIBuffer.Chamada = Call;
            OutputErroAPIBuffer.Erro = response.ErrorMessage;
            OutputErroAPIBuffer.StatusCode = (int)response.StatusCode;
            OutputErroAPIBuffer.StatusCodeDesc = response.StatusCode.ToString();
            OutputErroAPIBuffer.Content = response.Content;
        }
    }
}