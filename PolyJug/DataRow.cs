using System;
using System.Collections.Generic;

public class DataRow : Dictionary<string, object> {

    //todo: this needs to be ordered
    //todo: the datarow is going to need types as well
    public new object this[string column] {

        get {

            if (ContainsKey(column)) {

                return base[column];
            } else {

                return null;
            }
        }

        set {

            if (ContainsKey(column)) {

                base[column] = value;
            } else {

                Add(column, value);
            }
        }
    }

    public override string ToString() {

        var sb = "";
       
        var t = this.Values;

        //this does not honor order directly
        foreach(string key in Keys) {

           sb += this[key].ToString(); //columnType[key]; //gotta remember how to cast. or write my own extension that does it seemlessley
        }

        return sb;
    }
}