using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Data.SqlClient;
using helpers;
using helpers.extensions;
public class MSDatabase
{
    protected Logger Logger;

    protected string _sConnectionString;
    protected void OpenSqlConnection()
    {
        _cConnection = new SqlConnection(_sConnectionString);
    }
    protected SqlConnection _cConnection;

    public string ConnectionString
    {
        get
        {
            return _sConnectionString;
        }
        set
        {
            _sConnectionString = value;
            if (null != _cConnection)
                _cConnection.Close();
            _cConnection = null;
        }

    }
    public MSDatabase()
    {
        Logger = new Logger();
    }
    public Queue GetValues(string sSQLQuery)
    {
        Queue aqRetVal = new Queue();
        SqlDataReader cReader = null;
        try
        {
            if (_cConnection == null)
                OpenSqlConnection();
            else
                _cConnection.Close();
            SqlCommand cCommand =
                   new SqlCommand(sSQLQuery, _cConnection);
            _cConnection.Open();
            cReader = cCommand.ExecuteReader();

            Hashtable ahRow = new Hashtable();
            while (cReader.Read())
            {
                ahRow = new Hashtable();
                for (int i = 0; i < cReader.FieldCount; i++)
                    ahRow[cReader.GetName(i)] = cReader.GetValue(i);
                aqRetVal.Enqueue(ahRow);
            }

        }
        catch (Exception ex)
        {
            aqRetVal = null;
            Logger.WriteError(ex);
        }
        finally
        {
            if (cReader != null)
                cReader.Close();
            if (_cConnection != null)
                _cConnection.Close();

        }
        return aqRetVal;
    }
    public long GetID(string sSQLQuery)
    {
        long nRetVal = -1;
        try
        {
            Queue aDBValues = GetValues(sSQLQuery);
            Hashtable aHT = (Hashtable)aDBValues.Dequeue();
            object[] aTemp = new object[1];
            aHT.Values.CopyTo(aTemp,0);
			nRetVal = aTemp[0].ToID();
        }
        catch (Exception ex)
        {
            nRetVal = -1;
            Logger.WriteError(ex);
        }
        return nRetVal;
    }
    public int ExecuteNonQuery(string sSQLQuery)
    {
        SqlCommand cCommand;
        try
        {
            if (_cConnection == null)
                OpenSqlConnection();
            _cConnection.Open();
            cCommand = new SqlCommand(sSQLQuery, _cConnection);
            cCommand.ExecuteNonQuery();
            _cConnection.Close();
            return 1;
        }
        catch (Exception ex)
        {
            Logger.WriteError(ex);
        }
        return -1;
    }
    private long WriteValues(string sTable, Hashtable aValues, string sWhere)
    {
        string sSQLQuery = "";
        SqlCommand cCommand;
        try
        {
            if (_cConnection == null)
                OpenSqlConnection();
            string sValue = "";
            string sComma = "";
            if (sWhere.Length > 0)
            {
                sSQLQuery = "UPDATE " + sTable + " SET ";
                foreach (string sColName in aValues.Keys)
                {
                    sValue = aValues[sColName].ToString();
                    sSQLQuery += sComma + sColName + "=" + sValue;
                    sComma = ",";
                }
                sSQLQuery += " WHERE " + sWhere;
            }
            else
            {
                string sColNames = " (";
                string sValues = " VALUES (";
                foreach (string sColName in aValues.Keys)
                {
                    sValue = aValues[sColName].ToString();
                    sColNames += sComma + sColName;
                    sValues += sComma + sValue;
                    sComma = ",";
                }
                if (aValues.ContainsKey("id"))
                    sSQLQuery = "SET IDENTITY_INSERT " + sTable + " ON;";
                sSQLQuery += "INSERT INTO " + sTable + sColNames + ") " + sValues + ")";
                sSQLQuery += ";Select SCOPE_IDENTITY();";
                _cConnection.Open();
                cCommand =
                       new SqlCommand(sSQLQuery, _cConnection);
                string sId = cCommand.ExecuteScalar().ToString();
                long nId = -1;
                if (sId.Length > 0)
					nId = sId.ToID();
                _cConnection.Close();
                return nId;

            }
            _cConnection.Open();
            cCommand =
                   new SqlCommand(sSQLQuery, _cConnection);
            cCommand.ExecuteNonQuery();
            _cConnection.Close();
        }
        catch (Exception ex)
        {
            Logger.WriteError(ex);
        }
        return -1;
    }
    public long Insert(string sTable, Hashtable aValues)
    {
        return WriteValues(sTable, aValues, "");
    }
    public void Update(string sTable, Hashtable aValues, string sWhere)
    {
        WriteValues(sTable, aValues, sWhere);
    }
    public void Delete(string sTable, string sWhere)
    {
        try
        {
            OpenSqlConnection();
            SqlCommand cCommand =
                   new SqlCommand("DELETE FROM " + sTable + " WHERE " + sWhere, _cConnection);
            cCommand.Connection.Open();
            cCommand.ExecuteNonQuery();
            _cConnection.Close();
        }
        catch (Exception ex)
        {
            Logger.WriteError(ex);
        }
    }
}
