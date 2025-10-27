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

        /// <summary>
        /// Busca en la BD usando un DICCIONARIO de filtros (columnas específicas).
        /// </summary>
        public List<AuditRecord> FindHistoryByFilters(Dictionary<string, string> filters)
        {
            var historyRecords = new List<AuditRecord>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string baseQuery = @"SELECT PR_SEQ, PR_STAT, PR_REG_ANTX, PR_REG_ACTX, PR_FECHA, PR_HORA FROM dbo.TXAUDITORIA";
                var whereConditions = new List<string>();
                var parameters = new List<SqlParameter>();

                foreach (var filter in filters)
                {
                    string key = filter.Key; string value = filter.Value;
                    switch (key)
                    {
                        case "-fechaini": whereConditions.Add("PR_FECHA >= @FechaIni"); parameters.Add(new SqlParameter("@FechaIni", Convert.ToDecimal(value))); break;
                        case "-fechafin": whereConditions.Add("PR_FECHA <= @FechaFin"); parameters.Add(new SqlParameter("@FechaFin", Convert.ToDecimal(value))); break;
                        case "-programa": whereConditions.Add("PR_PRG = @Programa"); parameters.Add(new SqlParameter("@Programa", value)); break;
                        case "-usuario": whereConditions.Add("PR_USUARIO = @Usuario"); parameters.Add(new SqlParameter("@Usuario", value)); break;
                        case "-archivo": whereConditions.Add("PR_ARCH = @Archivo"); parameters.Add(new SqlParameter("@Archivo", value)); break;
                    }
                }
                string finalQuery = baseQuery + (whereConditions.Count > 0 ? " WHERE " + string.Join(" AND ", whereConditions) : "") + " ORDER BY PR_SEQ ASC";

                using (var command = new SqlCommand(finalQuery, connection))
                {
                    if (parameters.Count > 0) command.Parameters.AddRange(parameters.ToArray());
                    using (var reader = command.ExecuteReader())
                    { /* ... leer resultados ... */
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
                // Esta es la consulta original que busca dentro del texto
                string query = @"SELECT PR_SEQ, PR_STAT, PR_REG_ACTX, PR_REG_ACTX, PR_FECHA, PR_HORA FROM dbo.TXAUDITORIA WHERE PR_REG_ANTX LIKE @SearchTerm OR PR_REG_ACTX LIKE @SearchTerm ORDER BY PR_SEQ ASC";
                using (var command = new SqlCommand(query, connection))
                {
                    // Construye el término de búsqueda LIKE, ej: %="ALBA"%
                    string searchTerm = $"%=\"{value}\"%";
                    command.Parameters.AddWithValue("@SearchTerm", searchTerm);
                    using (var reader = command.ExecuteReader())
                    { /* ... leer resultados ... */
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
                Hora = reader.GetDecimal(5)
            });
        }
    }
}