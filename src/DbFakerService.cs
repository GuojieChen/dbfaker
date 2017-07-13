using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dbfaker.sqlserver;
using Microsoft.CSharp;
using NLog;
using ServiceStack.Common.Net30;
using ServiceStack.OrmLite;
using TestDataGenerator;

namespace dbfaker
{
    public class DbFakerService:IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private CommandOptions _options { get; set; }

        private long _ticks = DateTime.UtcNow.Ticks;
        private readonly ConcurrentQueue<Type> _fakerTypes = new ConcurrentQueue<Type>();
        private static readonly string[] MySqlKeys = new[] { "persist security info", "charset", "allow user variables" };

        public bool IsOver;
        public DbFakerService(CommandOptions options)
        {
            if(string.IsNullOrEmpty(options.ConnectionString))
                throw new ArgumentNullException(options.ConnectionString);

            this._options = options;
        }

        private CancellationToken ct = new CancellationToken();


        private void StartTask()
        {
            IList<Task> tasks = new List<Task>();
            for (int i = 0; i < _options.Workers; i++)
                tasks.Add(Task.Factory.StartNew(FakerTask, ct));

            Task.WaitAll(tasks.ToArray());
            IsOver = true;
        }

        private void FakerTask()
        {
            while (_fakerTypes.Any())
            {
                Type type;
                _fakerTypes.TryDequeue(out type);

                if (type == null)
                    return;

                _logger.Info($"{type.Name} Faker Start ... ");

                typeof(DbFakerService)
                    .GetMethod("Faker", BindingFlags.Public | BindingFlags.Instance)
                    .MakeGenericMethod(type)
                    .Invoke(this, null);

                _logger.Info($"{type.Name} Faker End ... ");
            }
        }


        public void Start()
        {
            _logger.Info("starting...");
            _logger.Debug($"ConnectionString:{_options.ConnectionString}");

            _logger.Info("逆向工程...");
            var poco = new SqlServerPoco(_options.ConnectionString);
            var tables = poco.GetDbTables();

            _logger.Debug($"发现 {tables.Count} 张表，开始逆向生成模型");

            IList<string> sourcecodelist = new List<string>();
            var ts = _options.Tables.Split(new char[] {','}).Select(item => item.ToLower());
            foreach (var table in tables)
            {
                if (_options.Tables == null || ts.Contains(table.Name.ToLower()))
                    sourcecodelist.Add(GeneratorSourceCode(table));
            }

            foreach (var type in GetType(sourcecodelist.ToArray()))
            {
                _fakerTypes.Enqueue(type);
            }

            StartTask();
        }

        private static IDbConnectionFactory GetDbConnectionFactory(string connectionString)
        {
            var items = connectionString.Split(';');

            if (items.Select(item => item.Split('=')).Any(ss => MySqlKeys.Contains(ss[0].ToLower())))
            {//mysql
                return new OrmLiteConnectionFactory(connectionString,MySqlDialect.Provider);
            }
            else //sqlserver
                return new OrmLiteConnectionFactory(connectionString,SqlServerDialect.Provider);
        }

        private static readonly int SizePerPage = 20 ;
         
        public void Faker<T>() where T : new()
        {
            var currentSize = 0; 
            var catelog = new Catalog();
            using (var db = GetDbConnectionFactory(this._options.ConnectionString).OpenDbConnection())
            {
                var errorCount = 0; 
                while (currentSize<_options.Count && errorCount<20)
                {
                    IList<T> ts = new List<T>();
                    for (int i = 0; i < SizePerPage && currentSize<=_options.Count; i++)
                    {
                        ts.Add(catelog.CreateInstance<T>());
                        currentSize++;
                    }
                    try
                    {
                        db.InsertAll<T>(ts);
                        _logger.Debug($"Faker Table Datas：{typeof(T).Name}  {currentSize}/{_options.Count}");
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                    }
                }
            }
        }

        private IEnumerable<Type> GetType(string[] sourcecodelist)
        {
            var compiler = new CSharpCodeProvider();//得到一个CSharp的编译器
            var cp = new CompilerParameters();

            cp.OutputAssembly = $"{AppDomain.CurrentDomain.BaseDirectory}{_ticks}.dll";

            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.ComponentModel.DataAnnotations.dll");
            cp.ReferencedAssemblies.Add($"{AppDomain.CurrentDomain.BaseDirectory}ServiceStack.Common.dll");
            cp.ReferencedAssemblies.Add($"{AppDomain.CurrentDomain.BaseDirectory}ServiceStack.OrmLite.dll");
            cp.ReferencedAssemblies.Add($"{AppDomain.CurrentDomain.BaseDirectory}ServiceStack.Interfaces.dll");
            cp.GenerateExecutable = false;//这是指示说我们输出的程序集是dll，而不是exe
            cp.GenerateInMemory = true; //这是指示在内存中创建该程序集

            File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}{_ticks}.txt", sourcecodelist);

            var result = compiler.CompileAssemblyFromSource(cp, sourcecodelist); //执行编译

            return result.CompiledAssembly.GetTypes();
        }

        /// <summary>
        /// 生成源码信息
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private string GeneratorSourceCode(DbTable table)
        {
            var classname = $"{table.SechemaName}_{table.Name}";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using ServiceStack.DataAnnotations;");

            sb.AppendLine("namespace dbmigration");
            sb.AppendLine("{");

            sb.AppendLine($"[Alias(\"{table.Name}\")]");
            sb.AppendLine($"public class {classname} ");
            sb.AppendLine(" {");

            foreach (var column in table.Columns)
            {
                if (Equals(table.PrimaryKeys.ColumnName, column.ColumnName))
                {
                    sb.AppendLine("[PrimaryKey]");
                    if (column.IsIdentity)
                        sb.AppendLine("[AutoIncrement]");
                }

                if (Equals(column.DataType.ToLower(), "string") && column.MaxLength > 0)
                    sb.AppendLine($"[StringLength({column.MaxLength})]");

                if (!string.IsNullOrEmpty(column.DefaultValue))
                    sb.AppendLine($"[Default(typeof({column.DataType}),\"{column.DefaultValue.Replace("(", "").Replace(")", "")}\")]");

                sb.Append($"public {column.DataType} {column.ColumnName} "); sb.AppendLine(" { get; set; }");
            }

            sb.AppendLine("}");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public void Dispose()
        {
            _logger.Debug("Dispose...");

            var tempdllfile = $"{AppDomain.CurrentDomain.BaseDirectory}{_ticks}.dll";
            var temptxtfile = $"{AppDomain.CurrentDomain.BaseDirectory}{_ticks}.txt";

            if (File.Exists(tempdllfile))
                File.Delete(tempdllfile);
            if (File.Exists(temptxtfile))
                File.Delete(temptxtfile);
        }
    }
}
