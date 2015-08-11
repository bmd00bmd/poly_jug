using System;
using PolyJug;
using System.Text;
using PolyJug.Extensions;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Data;

namespace PolyJug {

    class Program {

        delegate string CommandDelegate(string userInput);
        public enum DBMODE {NEGOTIATE, MYSQL, SQLITE, MSSQL};

        private static string THIS_FOLDER;

        //todo: config file
        #region config file ish
        private static DBMODE   MODE = DBMODE.NEGOTIATE;

        private static string   currentDatabaseName = "";

        private static string   QUERY_DELIMETER = ";"; //remember this in a config

        private static bool     IS_TEE_OUTPUT = false;

        private static string   TEE_FILE = string.Empty;

        private static byte     LOG_LEVEL = 3; /* none, normal, warnings, all*/

        //remember that these are non query actions
        private static Dictionary<string, CommandDelegate> systemCommands = new Dictionary<string, CommandDelegate>() {
        
            {"\\h", GetHelpText},
            {"\\D", SetDebugLevel},
            {"\\d", SetDelimeter},
            {"\\q", ExitPolyJug},
            {"\\m", SetDatabaseMode},
            {"\\t", SetIsTeeOutputToFalse},
            {"\\z", GetHelpText}, // this will just get passed to query if mysql. everyone else needs the fake show
            {"\\.", GetHelpText}, // this will also have to pass the file to each database type in order to process it. might need to know what type it is.
            {"\\s", GetPolyJugStatus}, //should also go to the server and get a status if applicable
            {"\\T", SetTeeOutputToFile},
            {"\\u", SelectDatabase},
            {"\\W", SetWarningOn},
            {"\\w", SetWarningOff},
            {"query", Query}
        };

        private static Dictionary<string, string> aliasToSystemCommands = new Dictionary<string,string>() {
            
            /* help */
            {"help", "\\h"},
            {"\\?", "\\h"},
            {"?", "\\h"},
            {"\\h", "\\h"},

            /* debug */
            {"debug", "\\D"},
            {"\\D", "\\D"},

            /* delimeter */
            {"delimeter", "\\d"},
            {"\\d", "\\d"},

            /* exit */
            {"exit", "\\q"},
            {"quit", "\\q"},
            {"\\q", "\\q"},

            /* mode */
            {"mode", "\\m"},
            {"\\m", "\\m"},

            /* no tee */
            {"no tee", "\\t"},
            {"notee", "\\t"},
            {"nolog", "\\t"},
            {"no log", "\\t"},
            {"noout", "\\t"},
            {"no out", "\\t"},
            {"\\t", "\\t"},

            /* show */
            {"show", "\\z"},

            /* script from source */
            {"source", "\\."},
            {"script", "\\."},
            {"load", "\\."},
            {"\\.", "\\."},

            /* status */
            {"status", "\\s"},
            {"server", "\\s"},
            {"\\s", "\\s"},
            
            /* tee */
            {"tee", "\\T"},
            {"log", "\\T"},
            {"out", "\\T"},
            {"\\T", "\\T"},

            /* use */
            {"use", "\\u"},
            {"database", "\\u"},
            {"\\u", "\\u"},
            
            /* warnings */
            {"warning", "\\W"},
            {"warnings", "\\W"},
            {"warn", "\\W"},
            {"\\W", "\\W"},

            /* no warnings */
            {"nowarning", "\\w"},
            {"nowarnings", "\\w"},
            {"nowarn", "\\w"},
            {"no warning", "\\w"},
            {"no warnings", "\\w"},
            {"no warn", "\\w"},
            {"\\w", "\\w"}
        };
        #endregion


        #region delegates
        private static string GetHelpText(string specificHelpTopic="") {
        //if there is a value in the specific help topic, then give specific help, otherwise, return the general help
                return
    @"
    ....:::: SYSTEM ::::....

    ?         (\?) Synonym for help'.
    debug     (\D) Set debug output from 0-3
    delimiter (\d) Set statement delimiter.
    exit      (\q) Exit PolyJug. Same as quit.
    mode      (\m) either mysql, sqlite or mssql
    no tee    (\t) Don't write into outfile.
    show      (  ) mimic show behavior in mysql
    source    (\.) Execute an SQL script file. Takes a file name as an argument.
    status    (\s) Get status information from the server.
    tee       (\T) Set outfile [to_outfile]. Append everything into given outfile.
    use       (\u) Use another database. Takes database name as argument.
    warnings  (\W) Show warnings after every statement. debug 2
    nowarn    (\w) Don't show warnings after every statement. debug 1
    
    ....:::: MID-QUERY ::::....

    clear     (\c) Clear the current input statement.
    e go      (\G) Send command to database, display results vertically.
    go        (; ) Send command to database.
    ";
        }

        private static string SetDebugLevel(string newLevel) {

            var report = "";

            if(newLevel.IsByte()) {
                
                LOG_LEVEL = Convert.ToByte(newLevel);

                report = "debug level set to " + newLevel;
            }else{
            
                report = "could not set debug level to "+ newLevel;
            }

            return report;
        }

        private static string SetWarningOn(string isTrue="") {       
            return SetDebugLevel("2");
        }

        private static string SetWarningOff(string isFalse="") {        
            return SetDebugLevel("1");
        }

        private static string SetDelimeter(string newDelimeter) {
        
            var report = "";

            //there may be other special symbols that i want to block. for now just make sure it is not blank
            if(newDelimeter.Length > 0) {
                
                QUERY_DELIMETER = newDelimeter;

                report = "query delimeter set to " + newDelimeter;
            }else{
            
                report = "could not set query delimeter to "+ newDelimeter;
            }

            return report;
        }

        private static string ExitPolyJug(string noArg="") {
            //this is cheap. breaks the pattern
            ConsoleLogLine(goodByeText, 1);
            System.Environment.Exit(1);
            return string.Empty;
        }

        private static string SetDatabaseMode(string newMode) {
        
            var report = "";
            var parseMode = MODE;

            if(Enum.TryParse<DBMODE>(newMode.ToUpper(), out parseMode)) {
                
                MODE = parseMode;

                report = "database mode set to " + newMode;
            }else{
            
                report = "could not set database mode to "+ newMode;
            }

            return report;
        }

        private static string SetIsTeeOutputToFalse(string isFalse="") {
        
            var report = "";
            
            if(IS_TEE_OUTPUT) {
                
                IS_TEE_OUTPUT = false;
                report = "no longer (tee)writting to file: " + teeFile;

            }else{
            
                report = "tee is already disabled";
            }

            return report;
        }

        private static string SetTeeOutputToFile(string newFile="") {
        
            var report = "";
            
            //todo: validate that the input is a good path with no symbols and no spaces, etc.
            TEE_FILE = newFile;

            if(IS_TEE_OUTPUT) {

                report = "changed (tee)writting to file: " + teeFile;

            }else{
                
                IS_TEE_OUTPUT = true;
                report = "now (tee)writting to: " + teeFile ;
            }

            return report;
        }

        private static string GetPolyJugStatus(string noArgs="") {
        
            return string.Format (
            @"
            Version: {0}
            Mode:    {1}
            Using:   {2}
            IsTee:   {3}
            Outfile: {4}
            Endline: {5}
            LogLevel {6}
            ",
            0, MODE.ToString(), currentDatabaseName, IS_TEE_OUTPUT, teeFile, QUERY_DELIMETER, LOG_LEVEL);
        }
        #endregion

        private static string SelectDatabase(string databaseName) {
        
            // if sqlite, then try to find the file, otherwise, just create it and use it.
            
            var report = "";

            if(MODE == DBMODE.SQLITE) {
            
                currentDatabaseName = databaseName;

                if(System.IO.File.Exists(databaseName)) {
                    
                    report = "using "+databaseName;
                }else{

                    report = string.Format("creating {0} because it does not exist and you are trying to use it", databaseName);
                    SQLiteConnection.CreateFile(databaseName);
                }
                
            }else{
                report = "sqlite is the only supported mode right now...";
            }

            return report;
        }

        private static string Query(string query) {
            
            var report = "";

            if(MODE == DBMODE.SQLITE) {
                //then actually do it

                if(currentDatabaseName.Length > 0) {
                    
                    var conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", currentDatabaseName));
                    conn.Open();

                    IDataReader results = null;
                    string errorText = string.Empty;

                    try{

                        results = new SQLiteCommand(query, conn).ExecuteReader();
                    
                    }catch(Exception e) {
                        
                        errorText = e.Message;
                    }

                    if(results != null) {
                        
                        //i must keep the same order. i could do dict int, dict name type... but meah
                        var columns = new string[results.FieldCount];
                        var columnTypes = new Type[results.FieldCount];

                        for(int i=0; i < results.FieldCount; ++i) {
                            columns[i] = results.GetName(i);
                            columnTypes[i] = results[columns[i]].GetType();
                        }

                        DataTable table = null;
                        
                        while(results.Read()) {

                            if(table == null) {
                                table = new DataTable(columns, columnTypes);}

                            var row = new DataRow();

                            foreach(string columnName in columns) {
                                row[columnName] = results[columnName];}

                            if(table != null) {
                                table.AddRow(row);}
                        }

                        if(table != null){
                            report = table.ToString();
                        }else{
                            report = results.RecordsAffected + " rows affected";}

                    }else{
                        report = errorText;}


                    conn.Close();

                }else{

                    report = "you need to select a database before you can execute a command. use \\u your_database_name";
                }


            }else{

                report = "currently only supporting sqlite mode...";
            }

            return report;
        }


        #region main program stuff
        public static void Main(string[] args) {

            THIS_FOLDER = GetThisFolder();

            ConsoleLogLine(welcomeText);
            ConsoleLogLine(args.PrintR(), 3);

            if(args.Length == 0) {

                InteractiveLoop();

            }else {
                //execute the single command, or batch of commands then quit

            }
        }

        private static void InteractiveLoop() {
            
            var user_input = string.Empty;

            bool is_exiting = false;//detect ctyl c
            
            while(!is_exiting) {

                if(user_input == string.Empty || user_input=="\n") {

                    user_input = string.Empty;
                    Console.Write("PJ> ");
                }else{
                
                    Console.Write(" -> ");
                }

                user_input += Console.ReadLine() + "\n";

                //ignore input that only contains newlines
                if(user_input == "\n") {
                    continue;}

                var parsedInput = ParseRawInput(user_input);

                if(aliasToSystemCommands.ContainsKey(parsedInput[0])) {

                    var actualCommand = aliasToSystemCommands[parsedInput[0]];
                    var potentialArgs = parsedInput.Length > 1 ? parsedInput[1] : "";
                    ConsoleLogLine(systemCommands[actualCommand](potentialArgs));
                    user_input = string.Empty;
                
                }else if(user_input.Contains(QUERY_DELIMETER)) {
                
                    //then send the query to the query handler (based on mode?) negotiate tries history in conf, but defaults to sqlite
                    ConsoleLogLine(systemCommands["query"](user_input));
                    user_input = string.Empty;
                    //i might* create a specific method for each database type. for now just do sqlite and just do one
                }

                //interpret and do stuff
                //connect correctly to sqlite 
                //have help text
                //connect correctly tyo mssql
                //find a way to capture cotrol+c escape and handle it gracefully in the program. might need events

            }
        }


        private static string[] ParseRawInput(string rawInput) {
            //todo: extension this mess

            var trySplit = rawInput.Split(' ');
            var cleanSplit = new string[trySplit.Length];

            for(int i=0; i < trySplit.Length; ++i) {
                cleanSplit[i] = trySplit[i].Replace("\n", "");}

            return cleanSplit;
        }

        private static string BatchExecute() {
        
            return "";
        }

        private static string TryEnvoke(string userCommand) {
            
            //dictionary commands gathered at start from config file. json that has supported commands
            //{"realCommand": ["all", "the", "various", "alias"]}
            // then convert to a reverse lookup dictionary

            var temp = "i dont have a method for this command:";

            temp += userCommand; //suggestionMethod that tried to find a similar command and suggest that

            var pureCommand = userCommand.Substring(0, (userCommand.Length-2));

            if(systemCommands.ContainsKey(pureCommand)) {
                
                systemCommands[userCommand](userCommand);
            }


            if(userCommand == "help\n") {
                temp = GetHelpText("");
            }

            if(userCommand == "exit\n") {
                Console.WriteLine(goodByeText);
                System.Environment.Exit(1);
            }
        
            return temp;
        }

        private static void HandleArgs() {

            //return a delegate method to envoke depending on the args
            // one of the methods will be responsible for an interactive mode.
            // all the others will be batch like commands

        }
        #endregion


        #region util
        //todo: util ish
        private static void ConsoleLog(string message, byte insignificance = 1) {
            
            if(insignificance <= LOG_LEVEL) {

                if(IS_TEE_OUTPUT) {

                    var path = TEE_FILE == string.Empty ? THIS_FOLDER+"polyjug.log" : TEE_FILE;
                    System.IO.File.AppendAllText(message, path);
                }

                Console.Write(message);
            }
        }

        private static void ConsoleLogLine(string message, byte insignificance = 1) {
            
            if(insignificance <= LOG_LEVEL) {

                if(IS_TEE_OUTPUT) {

                    var path = TEE_FILE == string.Empty ? THIS_FOLDER+"polyjug.log" : TEE_FILE;
                    System.IO.File.AppendAllText(message+"\n", path);
                }

                Console.WriteLine(message);
            }
        }

        private static string GetThisFolder() {
        
            var path_bits = System.Reflection.Assembly.GetEntryAssembly().Location.Split('\\');

            string simple_path_i_have_to_build_because_you_suck_microsoft = "";

            for(int i=0; i<(path_bits.Length-1); ++i) {
                simple_path_i_have_to_build_because_you_suck_microsoft += path_bits[i]+'\\';}

            return simple_path_i_have_to_build_because_you_suck_microsoft;
        }
        #endregion

        //properties

        private static string teeFile {
        
            get {
                return TEE_FILE == string.Empty ? (THIS_FOLDER + "polyjug.log") : TEE_FILE;
            }
        }
        private static string welcomeText {
        
            get {
        
                return "\n\t\tPolyJug\n\n";
            }
        }
        private static string goodByeText {
        
            get {
            
                var farewell = new string[] {
                    "Until we meet again!",
                    "But fate ordains that dearest friends must part. ~Edward Young",
                    "Goodbyes are not forever...",
                    "I know now is not the best time, but I think I love you...",
                    "I hope you die...",
                    "We are never getting back together...",
                    "This is the last time we are breaking up...",
                    "Farewell!",
                    "Bye Friend :)",
                    "Laters...",
                    "Peace out...",
                    "See you later alligator...",
                    "rm -rf *                                   just kidding...",
                    "if i had feelings, they'd be hurt...",
                    "I'll just sit tight till you come crawling back...",
                    "don't you just hate that sqlcmd..."
                };
                 
                return farewell[new Random().Next(0, farewell.Length)];            
            }
        }

    }
}