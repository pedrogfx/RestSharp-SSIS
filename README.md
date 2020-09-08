# Extracao-de-dados-RestSharp

Extração de dados da API do ZenDesk com a biblioteca RestSharp usando Componente Script do SSIS em C#.

Desenvolvimento feito atravez do SQL Server Integration Services (SSIS) utilizando processo de ETL.		


Em todos os components scripts, utilizamos a parte de "Inputs and Outputs" para criar a saídas da extração, criamos a saída como "OutputAPI", e dentro dessa categoria adicionamos os campos que desejamos extrair com os seus data type.

* OutputAPI:
  * Output Columns:
    * id : eight-byte signed integer [DT_I8];
    * created_at: database timestamp [DT_DBTIMESTAMP];
    * updated_at: database timestamp [DT_DBTIMESTAMP];
    * type: string [DT_STR];
![alt text](https://github.com/pedrogfx/Extracao-de-dados-RestSharp/blob/master/ZENDESK/TICKETS/Print%20output%20example.png)
    
Exemplo que como que ficaria a estrutura da extração da Tickets.

Dentro do script utilizamos parametros para algumas variáveis, para questão de segurança, temos um pacote que possui todas as variáveis que tenham qualquer sensibilidade (Acessos a DB's e Tokens).

Essas variáveis passamos do SSIS para dentro do Script como "Read only Variables".
		
