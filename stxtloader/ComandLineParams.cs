/*
 * Created by SharpDevelop.
 * User: user
 * Date: 24.07.2014
 * Time: 11:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Text.RegularExpressions;

namespace stxtloader
{
	/// <summary>
	/// Description of ComandLineParams.
	/// </summary>
	public class ComandLineParams
	{
		private const string strDelimiter = "="; // задается разделитель параметра
		private const string strHelp = @"
Использование:
    stxtloader [/s|/server=<имя сервера>][/ia|/intauth | /u|/user"+strDelimiter+@"<имя пользователя> /p|/password"+strDelimiter+@"<пароль>]
					[/od|/outdir"+strDelimiter+ @"<путь к директории>] [/at|/alltasks]
    

Параметры
	/s|/server			Имя сервера.    
	/ia|/intauth		Использовать встроенную аутентификацию Windows.
    /u|/user			Имя пользователя для аутентификации.
    /p|/password		Пароль для аутентификации.
    /od|/outdir			Путь к директории для сохранения файлов.
    /at|/alltasks       Обрабатывать все задания ( по умолчанию обрабатываются только снятые с контроля).
";
		
		
		private string [] strCommands = new string[]{
			@"/?", @"/h", @"/help"	// вызов справки
			,@"/s", @"/server"		// имя сервера для подключения
			,@"/ia", @"/intauth"	// встроенная аутентификация
			,@"/u", @"/user"	// имя пользователя
			,@"/p",@"/password"	// пароль
			,@"/od",@"/outdir"	// путь к директории
            ,@"/at",@"/alltasks"	// все задания в базе
		};
		private enum ArgsPositions {
				ArgPosHelpQuestionSign = 0, ArgPosH,ArgPosHelp
				,ArgPosS,ArgPosServer
				,ArgPosIa,ArgPosIntauth
				,ArgPosU,ArgPosUser
				,ArgPosP,ArgPosPassword
				,ArgPosOd,ArgPosOutdir
                ,ArgPosAt,ArgPosAlltasks
				,ArgError
		}
		private enum ParamsInArgsPosition{
				ParamName = 0, Value
		}
		public bool bParseSuccess {get;set;}
		public bool bIntAuth {get;set;}
        public bool bAllTasks { get; set; }
		public string strServer {get;set;}
		public string strUser {get;set;}
		public string strPassword {get;set;}
		public string strPathToOutDir {get;set;}
		
		public ComandLineParams(string [] args)
		{
			ArgsPositions curArgPos;
			bParseSuccess = true;
			bIntAuth = false;
            bAllTasks = false;
			string [] strCommandParts = null;
			if( args.Length < 1){
				Console.Write(strHelp);
				return;
			}
			foreach ( string arg in args )
			{
				bool bHasParam = arg.Contains(strDelimiter);
				string strCurCommand;
				curArgPos = ArgsPositions.ArgPosHelpQuestionSign;
				foreach ( string strCommand in strCommands )
				{
					if (bHasParam){
						strCommandParts = Regex.Split(arg,strDelimiter);
						strCurCommand = strCommandParts[(int)ParamsInArgsPosition.ParamName];
					}else{
						strCurCommand = arg;
					}
					if ( strCurCommand == strCommand ) break;
					curArgPos += 1;
				}
				if (strCommandParts == null) curArgPos = ArgsPositions.ArgError;
				switch (curArgPos){
					case ArgsPositions.ArgPosHelpQuestionSign:
					case ArgsPositions.ArgPosH:	
					case ArgsPositions.ArgPosHelp:
						Console.Write(strHelp);
						bParseSuccess = false;
						break;
					case ArgsPositions.ArgPosIa:
					case ArgsPositions.ArgPosIntauth:
						bIntAuth = true;
						break;
					case ArgsPositions.ArgPosS:
					case ArgsPositions.ArgPosServer:
						strServer = strCommandParts[(int)ParamsInArgsPosition.Value];
						break;
					case ArgsPositions.ArgPosU:
					case ArgsPositions.ArgPosUser:
						strUser = strCommandParts[(int)ParamsInArgsPosition.Value];
						bIntAuth = false;
						break;
					case ArgsPositions.ArgPosP:
					case ArgsPositions.ArgPosPassword:
						strPassword = strCommandParts[(int)ParamsInArgsPosition.Value];
						bIntAuth = false;
						break;
					case ArgsPositions.ArgPosOd:
					case ArgsPositions.ArgPosOutdir:
						strPathToOutDir = strCommandParts[(int)ParamsInArgsPosition.Value];
						break;
                    case ArgsPositions.ArgPosAt:
                    case ArgsPositions.ArgPosAlltasks:
                        bAllTasks = true;
                        break;
					default:
                        Console.WriteLine("Неопознаный параметр.");
						Console.Write(strHelp);
						bParseSuccess = false;
						return;
						//break;
				}
			}
		}
	}
}
