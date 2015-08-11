using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

public class DataTable {

    public DataTable(string[] columnNames, Type[] columnTypes) {

        Columns = new List<string>(columnNames);
        ColumnTypes = new Dictionary<string, Type>();

        for(int i=0; i<columnNames.Length; ++i) {
            ColumnTypes.Add(columnNames[i], columnTypes[i]);}

        //if the Columns.length != ColumnTypes.length is not the sdame, the error

        Rows = new List<DataRow>();
    }
    public List<string> Columns { get; set; }
    public Dictionary<string, Type> ColumnTypes { get; set; }
    public List<DataRow> Rows { get; set; }
    public DataRow this[int row] {

        get {
            return Rows[row];
        }
    }
    public void AddRow(object[] values) {

        if (values.Length != Columns.Count) {

            throw new IndexOutOfRangeException("The number of values in the row must match the number of column");
        }

        var row = new DataRow();

        for (int i = 0; i < values.Length; i++) {

            row[Columns[i]] = values[i];
        }

        Rows.Add(row);
    }
    public void AddRow(DataRow newRow) {
        //check the column types and length first
        Rows.Add(newRow);
    }

    public override string ToString() {

        var sb = Columns.ToString() + "\n";
        //some other formatting potentially

        foreach(DataRow row in Rows) {
            sb += row.ToString() + "\n";}

        return sb;
    }
}