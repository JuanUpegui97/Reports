using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace AnalizadorAuditoria.Methods
{
    public class AuditHistoryFinder
    {
        private readonly string _connectionString;

        public AuditHistoryFinder(string connectionString)
        {
            _connectionString = connectionString;
        }


        private void UnlockTable(SqlConnection connection)
        {
            using (SqlCommand authCmd = new SqlCommand("EXEC sp_set_session_context @key, @value", connection))
            {
                authCmd.Parameters.AddWithValue("@key", "clave");
                authCmd.Parameters.AddWithValue("@value", "ACTIVO");
                authCmd.ExecuteNonQuery();
            }

        }
        /// <summary>
        /// Busca en la BD usando un DICCIONARIO de filtros (columnas específicas).
        /// </summary>
        public List<AuditRecord> FindHistoryByFilters(Dictionary<string, string> filters)
        {
            //Va almacenar todos los registros encontrados segun la consulta 
            var historyRecords = new List<AuditRecord>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                UnlockTable(connection);
              
                // Variables para guardar las condiciones, parametros y cuerpo de la consulta
                string baseQuery = @"SELECT PR_SEQ, PR_STAT, PR_REG_ANTX, PR_REG_ACTX, PR_FECHA, PR_HORA, PR_ARCH FROM dbo.TXAUDITORIA";
                var whereConditions = new List<string>();
                var parameters = new List<SqlParameter>();

                //Va leer todos los parametros que ingreso el usuario para crear la consulta
                foreach (var filter in filters)
                {
                    
                    string key = filter.Key; 
                    string value = filter.Value;
                    switch (key)
                    {
                        case "-fechaini": whereConditions.Add("PR_FECHA >= @FechaIni"); parameters.Add(new SqlParameter("@FechaIni", Convert.ToDecimal(value))); break;
                        case "-fechafin": whereConditions.Add("PR_FECHA <= @FechaFin"); parameters.Add(new SqlParameter("@FechaFin", Convert.ToDecimal(value))); break;
                        case "-programa": whereConditions.Add("PR_PRG = @Programa"); parameters.Add(new SqlParameter("@Programa", value)); break;
                        case "-usuarioadm": whereConditions.Add("PR_USUARIO = @usuarioadm"); parameters.Add(new SqlParameter("@usuarioadm", value)); break;
                        case "-archivo": whereConditions.Add("PR_ARCH = @Archivo"); parameters.Add(new SqlParameter("@Archivo", value)); break;
                        case "-estado": whereConditions.Add("PR_STAT = @Estado"); parameters.Add(new SqlParameter("@Estado", value)); break;

                    }
                }
                //Se arma la consulta completa para traer la ingformacion de BD
                string finalQuery = baseQuery + (whereConditions.Count > 0 ? " WHERE " + string.Join(" AND ", whereConditions) : "") + " ORDER BY PR_SEQ ASC";

                //Se cogen los parametros y se ejecuta la consulta BD y esta trae la informacion en una lista
                using (var command = new SqlCommand(finalQuery, connection))
                {
                    if (parameters.Count > 0) command.Parameters.AddRange(parameters.ToArray());
                    using (var reader = command.ExecuteReader())
                    { 
                        while (reader.Read()) { AddAuditRecord(historyRecords, reader); }
                    }
                }
            }
            return historyRecords;
        }

        /// <summary>
        /// Busca en la BD usando LIKE en los campos XML/Texto.
        /// </summary>
        public List<AuditRecord> FindHistoryByXmlLikeSearch(string value)
        {
            var historyRecords = new List<AuditRecord>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                UnlockTable(connection);

                // Esta es la consulta original que busca dentro del texto
                string query = @"SELECT PR_SEQ, PR_STAT, PR_REG_ANTX, PR_REG_ACTX, PR_FECHA, PR_HORA, PR_ARCH FROM dbo.TXAUDITORIA WHERE PR_REG_ANTX LIKE @SearchTerm OR PR_REG_ACTX LIKE @SearchTerm ORDER BY PR_SEQ ASC";
                using (var command = new SqlCommand(query, connection))
                {
                    // Construye el término de búsqueda LIKE, ej: %="ALBA"%
                    string searchTerm = $"%=\"{value}\"%";
                    command.Parameters.AddWithValue("@SearchTerm", searchTerm);
                    using (var reader = command.ExecuteReader())
                    { 
                        while (reader.Read()) { AddAuditRecord(historyRecords, reader); }
                    }
                }
            }
            return historyRecords;
        }

        /// <summary>
        /// Método privado auxiliar para evitar repetir el código de lectura del reader.
        /// </summary>
        private void AddAuditRecord(List<AuditRecord> list, SqlDataReader reader)
        {
            list.Add(new AuditRecord
            {
                Id = (int)reader.GetDecimal(0),
                Status = reader.GetString(1),
                XmlOld = reader.IsDBNull(2) ? null : reader.GetString(2),
                XmlNew = reader.IsDBNull(3) ? null : reader.GetString(3),
                Fecha = reader.GetDecimal(4),
                Hora = reader.GetDecimal(5),
                Archivo = reader.GetString(6),
            });
        }
    }
}