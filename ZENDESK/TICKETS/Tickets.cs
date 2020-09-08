using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Microsoft.CSharp;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

[Microsoft.SqlServer.Dts.Pipeline.SSISScriptComponentEntryPointAttribute]

public class ScriptMain : UserComponent

{
    //Criamos as devidas classes de acordo com os dados que queremos fazer a extração e também com a exata nomenclatura da API, pois o RestSharp faz o apontamento dos dados por associação. 
    Int64 EndTime = 0;
    public class CustomFields {
        public Int64 id { get; set; }
        public string value { get; set; }
    }

    public class Detalhes {
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

    //Alguns casos vamos precisar criar Objetos, como podemos ver no detalhes
    public class Tickets {
        public Detalhes[] tickets { get; set; }
        public bool end_of_stream { get; set; }
        public Int64 end_time { get; set; }
    }

    public override void PreExecute ()

    {
        base.PreExecute ();
    }

    public override void PostExecute ()

    {

        base.PostExecute ();
        //Variavel de ambiente que criamos para definir o range final de extração dos dados.
        this.ReadWriteVariables["User::EndTime"].Value = EndTime;

    }

    public override void CreateNewOutputRows ()

    {
        //Alguns protocolos de segurança que devemos utilizar para a extração vir completa se nenhum dado NULL
        ServicePointManager.Expect100Continue = true;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        string Url = Variables.ZendeskUrl; //URL DO ZENDESK.
        string User = Variables.ZendeskUser; //USUÁRIO DE ACESSO.
        string Pass = Variables.ZendeskPass; //SENHA DE ACESSO.
        Int64 StartTime = Variables.FuncTime; //VARIAVEL QUE PASSAMOS POR PARAMETRO NO PACOTE PARA DECLARAR A DATA DE INICIO DA EXTRAÇÃO.
        string Call = Url + "incremental/tickets.json?per_page=1000&start_time=" + StartTime.ToString (); //FUNÇÃO DE CHAMADA DA API

        var client = new RestClient (Call);

        client.Timeout = -1;
        client.Authenticator = new HttpBasicAuthenticator (User, Pass);

        var request = new RestRequest (Method.GET);
        IRestResponse response = client.Execute (request);

        if (response.IsSuccessful) { //AQUI TEMOS UM LOG DE ERRO ONDE TESTAMOS A CONEXÃO DA API ANTES DE COMEÇAR A EXTRAÇÃO.
            var settings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var APIDetalhes = JsonConvert.DeserializeObject<Tickets> (response.Content, settings); //DESSERIALIZAÇÃO DA ESTRUTURA DO JSON
            //API DO ZENDESK TRABALHA COM O UNIXTIME, DENTRO DO NOSSO PACOTE DO SSIS TEMOS UM FOR/LOOPING CONTAINER ONDE @FUNCTIME = DATAINICIOUNIX (RANGE DE DATA QUE QUERMEOS)
            //E FAZ O LOOP COM A CONDIÇÃO DE @FUNTIME < DATAFIMUNIX
            if (APIDetalhes.end_of_stream) {
                EndTime = Variables.DataFimUnix;
            } else {
                EndTime = APIDetalhes.end_time;
            }

            //TEMOS QUE FAZER UM FOREACH PARA A NOSSA DESSERIALIZAÇÃO APIDETALHES
            //E AQUI VAMOS ENTRANDO NO DETALHE DE ACORDO COM A NECESSIDADE DO USUÁRIO PARA FAZER A EXTRAÇÃO
            //ADICIONAMOS UM "OutputAPIBuffer.AddRow ();" PARA PODER ADICIONAR UMA LINHA DE REGISTRO PARA CADA UM DOS OUTPUT
            //O OUTPUTAPIBUFFER.URL ESTA RECEBENDO O DADO DA API ATRAVEZ DO FOR E DA VARIAVEL DET.URL ONDE ESTA SENDO ATRBUÍDA ESTE VAlor;
            foreach (var Det in APIDetalhes.tickets) {
                if (Det.custom_fields.Count > 0) {
                    foreach (var cf in Det.custom_fields) {
                        if (cf.value != "null" && cf.value != "NULL" && cf.value != null) {
                            OutputAPIBuffer.AddRow ();
                            OutputAPIBuffer.url = Det.url;
                            OutputAPIBuffer.id = Det.id;
                            OutputAPIBuffer.createdat = Det.created_at.ToLocalTime ();
                            OutputAPIBuffer.updatedat = Det.updated_at.ToLocalTime ();
                            OutputAPIBuffer.type = Det.type;
                            OutputAPIBuffer.subject = Det.subject;
                            OutputAPIBuffer.rawsubject = Det.raw_subject;
                            if (Det.description.Length >= 8000) {
                                OutputAPIBuffer.description = Det.description.Substring (0, 8000);
                            } else {
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
                            OutputAPIBuffer.tags = string.Join (",", Det.tags);
                            OutputAPIBuffer.collaboratorids = string.Join (",", Det.collaborator_ids);
                            OutputAPIBuffer.followerids = string.Join (",", Det.follower_ids);
                            OutputAPIBuffer.emailccids = string.Join (",", Det.email_cc_ids);
                        }

                    }
                } else {
                    //AQUI TEMOS A CONDIÇÃO CASO NÃO SE ENCAIXE NOS CUSTOMFIELDS > 0 E TEREMOS QUE ADICIONAR OUTRA LINHA DE REGISTRO COM O ADDROW();
                    OutputAPIBuffer.AddRow ();
                    OutputAPIBuffer.url = Det.url;
                    OutputAPIBuffer.id = Det.id;
                    OutputAPIBuffer.createdat = Det.created_at.ToLocalTime ();
                    OutputAPIBuffer.updatedat = Det.updated_at.ToLocalTime ();
                    OutputAPIBuffer.type = Det.type;
                    OutputAPIBuffer.subject = Det.subject;
                    OutputAPIBuffer.rawsubject = Det.raw_subject;
                    if (Det.description.Length >= 8000) {
                        OutputAPIBuffer.description = Det.description.Substring (0, 8000);
                    } else {
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
        } else {
            //CASO NÃO HAJA CONEXÃO COM API, TEMOS ESTE LOG DE ERRO QUE SERA APONTADO PARA OUTRO "OUTPUTAPI"
            OutputErroAPIBuffer.AddRow ();
            OutputErroAPIBuffer.Chamada = Call;
            OutputErroAPIBuffer.Erro = response.ErrorMessage;
            OutputErroAPIBuffer.StatusCode = (int) response.StatusCode;
            OutputErroAPIBuffer.StatusCodeDesc = response.StatusCode.ToString ();
            OutputErroAPIBuffer.Content = response.Content;
        }
    }
}