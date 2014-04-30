using System;
using System.Data;
using System.Data.SqlServerCe;
using System.Data.SqlTypes;

namespace FeedCenter.Data
{
    public static class Extensions
    {
        #region SqlDateTime

        public static SqlDateTime SqlDateTimeZero = new SqlDateTime(0, 0);

        #endregion

        #region DataSet

        public static DataRow GetFirstDataRow(this DataSet dataSet)
        {
            // If we get no data set then return nothing
            if (dataSet == null)
                return null;

            // If there were no tables returns then return nothing
            if (dataSet.Tables.Count == 0)
                return null;

            // Get the first table
            DataTable firstTable = dataSet.Tables[0];

            // If the table has no rows then return nothing
            if (firstTable.Rows.Count == 0)
                return null;

            // Return the first row
            return firstTable.Rows[0];
        }

        #endregion

        #region SqlCeCommand

        public static void SetStatement(this SqlCeCommand command, string statement, params object[] parameters)
        {
            // Create a new array to hold the updated parameters
            object[] formattedParameters = new object[parameters.Length];

            // Initialize our position
            int position = 0;

            // Loop over each parameter
            foreach (object parameter in parameters)
            {
                // If the parameter is a DateTime then we need to reformat
                if (parameter == null)
                {
                    // Use a explicit null value
                    formattedParameters[position++] = "NULL";
                }
                else if (parameter is DateTime)
                {
                    // Cast the parameter back to a DateTime
                    DateTime dateTime = (DateTime) parameter;

                    // Convert the DateTime to sortable format
                    string formatted = dateTime.ToString("s");

                    // Set into the formatted array
                    formattedParameters[position++] = formatted;
                }
                else if (parameter is bool)
                {
                    // Convert the boolean to a number
                    formattedParameters[position++] = Convert.ToInt32(parameter);
                }
                else if (parameter.GetType().IsEnum)
                {
                    // Convert the enum to a number
                    formattedParameters[position++] = Convert.ToInt32(parameter);
                }
                else if (parameter is string)
                {
                    // Escape single quotes
                    formattedParameters[position++] = (parameter as string).Replace("'", "''");
                }
                else
                {
                    // Just put the original value in
                    formattedParameters[position++] = parameter;
                }
            }

            // Build the full statement
            command.CommandText = string.Format(statement, formattedParameters);
        }

        #endregion

        #region SqlCeConnection

        public static void ExecuteNonQuery(this SqlCeConnection connection, string query, params object[] parameters)
        {
            // Create the command object 
            SqlCeCommand command = connection.CreateCommand();

            // Set the statement based on the query and parameters
            command.SetStatement(query, parameters);

            //Tracer.WriteLine("Executing SQL statement: {0}", command.CommandText);

            // Execute the command
            command.ExecuteNonQuery();
        }

        public static DataSet ExecuteDataSet(this SqlCeConnection connection, string query, params object[] parameters)
        {
            // Create the command object
            SqlCeCommand command = connection.CreateCommand();

            // Set the statement based on the query and parameters
            command.SetStatement(query, parameters);

            // Create a new data adapter
            using (SqlCeDataAdapter adapter = new SqlCeDataAdapter(command))
            {
                // Create the new data set
                using (DataSet dataSet = new DataSet())
                {
                    //Tracer.WriteLine("Executing SQL query: {0}", command.CommandText);

                    // Fill the data set
                    adapter.Fill(dataSet);

                    return dataSet;
                }
            }
        }

        public static object ExecuteScalar(this SqlCeConnection connection, string query, params object[] parameters)
        {
            // Create the command object 
            SqlCeCommand command = connection.CreateCommand();

            // Set the statement based on the query and parameters
            command.SetStatement(query, parameters);

            //Tracer.WriteLine("Executing SQL statement: {0}", command.CommandText);

            // Execute the command
            return command.ExecuteScalar();
        }

        #endregion
    }
}
