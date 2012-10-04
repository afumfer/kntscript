using System.Collections.Generic;
using System.Reflection;

using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;


namespace AnTScript
{
    public sealed class CodeRun
    {
        #region Fields

        // TODO: Esto deber�a ser un HashTable
        Dictionary<string, object> symbolTable;
        
        bool flagBreak = false;
        
        // TODO: El tipo de objeto para la salida deber�a ser un objeto abstracto y
        //       la instancia concreta deber�a venir v�a inyecci�n de c�digo.
        //       Ahora est� as� para salir del paso.
        //       (Arreglar esto en el futuro).
        TextBox textOut;
                
        #endregion

        #region Properties

        private object _defaultFunctionLibrary;
        public object DefaultFunctionLibrary
        {
            get 
            {
                if (_defaultFunctionLibraryType == null || _defaultFunctionLibrary == null)
                {
                    _defaultFunctionLibraryType = Type.GetType("AnTScript.Library", false, true);
                    _defaultFunctionLibrary = Activator.CreateInstance(_defaultFunctionLibraryType);
                }
                return _defaultFunctionLibrary;
            }
            set
            {
                _defaultFunctionLibrary = value;
                _defaultFunctionLibraryType = _defaultFunctionLibrary.GetType();
            }

        }

        private Type _defaultFunctionLibraryType;
        public Type DefaultFunctionLibraryType
        {
            get
            {
                if (_defaultFunctionLibraryType == null || _defaultFunctionLibrary == null)
                {
                    _defaultFunctionLibraryType = Type.GetType("AnTScript.Library", false, true);
                    _defaultFunctionLibrary = Activator.CreateInstance(_defaultFunctionLibraryType);
                }
                return _defaultFunctionLibraryType;
            }
        }

        #endregion

        #region Constructor

        public CodeRun(Stmt stmt, TextBox textOut)
        {
            this.textOut = textOut;
            symbolTable = new Dictionary<string, object>();

            // Go Run!
            RunStmt(stmt);
        }

        #endregion

        #region CodeRun main methods

        private void RunStmt(Stmt stmt)
        {
            if (flagBreak)
                return;

            if (stmt is Sequence)
            {
                Sequence seq = (Sequence)stmt;
                RunStmt(seq.First);
                RunStmt(seq.Second);                
            }

            else if (stmt is DeclareVar)
            {
                // declare
                DeclareVar declare = (DeclareVar)stmt;

                CodeDeclareSymbol(declare);

                //// set the initial value, no hace falta, ya se hace en CodDeclare,
                //// esto es otra forma de hacerlo.
                //Assign assign = new Assign();
                //assign.Ident = declare.Ident;
                //assign.Expr = declare.Expr;
                //RunStmt(assign);
            }

            else if (stmt is Assign)
            {
                Assign assign = (Assign)stmt;
                CodeStoreSymbol(assign);
            }

            else if (stmt is Print)
            {
                Print print = (Print)stmt;
                CodeExecutePrint(print);
            }

            else if (stmt is FunctionStmt)
            {
                FunctionStmt fun = (FunctionStmt)stmt;
                CodeExecuteFunction(fun.Function);
            }

            else if (stmt is ReadNum)
            {
                ReadNum read = (ReadNum)stmt;

                Assign assign = new Assign();
                //assign.Identifier = read.Identifier;
                assign.Ident = read.Ident;

                DoubleVal decLiteral = new DoubleVal();
                decLiteral.Value = CodeExecuteReadNum();

                assign.Expr = decLiteral;
                RunStmt(assign);
            }

            //flagBreak
            else if (stmt is BreakStmt)
            {
                flagBreak = true;
                return;
            }

            else if (stmt is ForLoop)
            {
                // example: 
                // for x = 0 to 100 do
                //   print "hello";
                // end for;

                ForLoop forLoop = (ForLoop)stmt;

                IntVal numFrom = new IntVal();
                IntVal numTo = new IntVal();

                Assign assignFrom = new Assign();                
                assignFrom.Ident = forLoop.Ident;

                assignFrom.Expr = forLoop.From;
                RunStmt(assignFrom);

                numFrom.Value = Convert.ToInt32(GenExpr(forLoop.From));
                numTo.Value = Convert.ToInt32(GenExpr(forLoop.To));

                while (numFrom.Value <= numTo.Value)
                {
                    if (flagBreak)
                        break;
                    RunStmt(forLoop.Body);
                    numFrom.Value++;
                    assignFrom.Expr = numFrom;
                    RunStmt(assignFrom);
                }
                if (flagBreak)
                    flagBreak = false;
            }

            else if (stmt is IfStmt)
            {
                // example: 
                // if a == 10 then 
                //   print "hello";
                // else
                //   print "bye";
                // end if;

                IfStmt ifStmt = (IfStmt)stmt;
                IntVal ifExp = new IntVal();

                ifExp.Value = Convert.ToInt32(GenExpr(ifStmt.TestExpr));

                if (ifExp.Value != 0)
                {
                    RunStmt(ifStmt.BodyIf);
                }
                else
                {
                    if (ifStmt.DoElse)
                        RunStmt(ifStmt.BodyElse);
                }
            }

            else if (stmt is WhileStmt)
            {
                // example: 
                // while a <= 10
                //   print "hello";
                //   a = a + 1;
                // end while;

                WhileStmt whileStmt = (WhileStmt)stmt;
                IntVal whileExp = new IntVal();

                while (true)
                {
                    if (flagBreak)
                        break;
                    whileExp.Value = Convert.ToInt32(GenExpr(whileStmt.TestExpr));
                    if (whileExp.Value == 0)
                        break;
                    RunStmt(whileStmt.Body);
                }
                if (flagBreak)
                    flagBreak = false;
            }

            else
            {
                throw new System.Exception("don't know how to gen a " + stmt.GetType().Name);
            }

        } 

        private object GenExpr(Expr expr) 
        {
            // ...
            // TODO: pendiente de implmentar la comprobaci�n de tipos.
            // A�adir el par�metro, ... System.Type expectedType como par�metro de entrada
            // de este m�todo.
            // ...

            Type deliveredType;
            object res = null;

            if (expr is StringVal)                         
                res = ((StringVal)expr).Value;

            else if (expr is IntVal)
                res = ((IntVal)expr).Value;
        
            else if (expr is FloatVal)
                res = ((FloatVal)expr).Value;

            else if (expr is DoubleVal)
                res = ((DoubleVal)expr).Value;

            else if (expr is DecimalVal)
                res = ((DecimalVal)expr).Value;

            else if (expr is DateTimeVal)                        
                res = ((DateTimeVal)expr).Value;

            else if (expr is Variable)                                        
                res = CodeReadSymbol((Variable)expr);

            else if (expr is FunctionExpr)                        
                res = CodeExecuteFunction((FunctionExpr)expr);

            else if (expr is NewObjectExpr)                       
                res = CodeExecuteNewObject((NewObjectExpr)expr);

            else if (expr is BinaryExpr)                        
                res = CodeExecuteBinaryExpr((BinaryExpr)expr);

            else if (expr is UnaryExpr)                            
                res = CodeExecuteUnaryExpr((UnaryExpr)expr);

                        
            // TODO: Pendiente, para resolver conversi�n de tipos autom�rica en versiones futuras 
            deliveredType = res.GetType();
            //if (deliveredType != expectedType)
            //{
            //    if (deliveredType == typeof(int) &&
            //        expectedType == typeof(string))
            //    {
            //        // Aqu� hacer el casting
            //        // ....
            //    }
            //    else
            //    {
            //        throw new System.Exception("can't coerce a " + deliveredType.Name + " to a " + expectedType.Name);
            //    }
            //} 

            return res;            

        } 

        #endregion

        #region Code execute region

        private void CodeDeclareSymbol(DeclareVar declare)
        {
            IdentObject ident = new IdentObject(declare.Ident);

            if (!string.IsNullOrEmpty(ident.Member))
                throw new System.Exception(" variable declaration '" + declare.Ident + "' incorrect ");

            if (!this.symbolTable.ContainsKey(ident.Obj))
                symbolTable.Add(declare.Ident, GenExpr(declare.Expr));
            else
                throw new System.Exception(" variable '" + ident.Obj + "' already declared");
        }

        private void CodeStoreSymbol(Assign assign)
        {
            IdentObject ident = new IdentObject(assign.Ident);

            if (this.symbolTable.ContainsKey(ident.Obj))
                if (string.IsNullOrEmpty(ident.Member))
                    symbolTable[ident.Obj] = GenExpr(assign.Expr);
                else
                {
                    try
                    {
                        object objNewValue;
                        objNewValue = GenExpr(assign.Expr); ;
                        SetValue(symbolTable[ident.Obj], ident, objNewValue);                       
                    }
                    catch (Exception ex)
                    {
                        throw new System.Exception(" error in assign code  '" + ident.Member + " :" + ex.Message);
                    }
                }
            else
                throw new System.Exception(" undeclared variable '" + ident.Obj);

        }

        private object CodeReadSymbol(Variable variable)
        {
            IdentObject ident = new IdentObject(variable.Ident);

            if (this.symbolTable.ContainsKey(ident.Obj))
                if (string.IsNullOrEmpty(ident.Member))
                    return symbolTable[ident.Obj];
                else
                {
                    try
                    {
                        object resGetValue;
                        GetValue(symbolTable[ident.Obj], ident, out resGetValue);
                        return resGetValue;
                    }
                    catch (Exception ex)
                    {
                        throw new System.Exception(" error in read code  '" + ident.Member + " :" + ex.Message);
                    }
                }
            else
                throw new System.Exception(" undeclared variable '" + ident.Obj);

        }

        private object CodeExecuteFunction(FunctionExpr function)
        {
            try
            {
                Type t;
                object obj;
                object objRoot;
                string funName;
                MethodInfo mi;

                IdentObject ident = new IdentObject(function.FunctionName);
                              
                if (string.IsNullOrEmpty(ident.Member))
                {
                    t = DefaultFunctionLibraryType;
                    obj = DefaultFunctionLibrary;
                    funName = ident.Obj;
                    mi = t.GetMethod(funName);
                }
                else
                {                    
                    objRoot = symbolTable[ident.Obj];                                        
                    GetObjectMethod(objRoot, ident, out obj, out mi);
                }
                                
                // Params
                object[] param;
                if (function.Args.Count > 0)
                {
                    param = new object[function.Args.Count];
                    for (int i = 0; i < function.Args.Count; i++)
                    {
                        param[i] = GenExpr(function.Args[i]);
                    }
                }
                else
                    param = null;
                                
                object ret;
                ret = mi.Invoke(obj, param);
                if (ret != null)
                    return ret;
                else
                    return 1;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private object CodeExecuteNewObject(NewObjectExpr newObject)
        {
            try
            {
                Type t;
                                                
                t = Type.GetType(newObject.ClassName, false, true);
            
                // Params
                object[] param;
                if (newObject.Args.Count > 0)
                {
                    param = new object[newObject.Args.Count];
                    for (int i = 0; i < newObject.Args.Count; i++)
                    {
                        param[i] = GenExpr(newObject.Args[i]);
                    }
                }
                else
                    param = null;

                return Activator.CreateInstance(t, param);   

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private object CodeExecuteBinaryExpr(BinaryExpr be)
        {
            object res = null;
            object left = GenExpr(be.Left);
            object right = GenExpr(be.Right);

            Type typeBinExpr = TypeOfBinaryExpr(left, right);
           
            if (typeBinExpr == typeof(int))
                res = CodeExecuteBinaryExpr<int>(left, right, be.Op);
            else if (typeBinExpr == typeof(float))
                res = CodeExecuteBinaryExpr<float>(left, right, be.Op);
            else if (typeBinExpr == typeof(double))
                res = CodeExecuteBinaryExpr<double>(left, right, be.Op);
            else if (typeBinExpr == typeof(decimal))
                res = CodeExecuteBinaryExpr<decimal>(left, right, be.Op);
            else if (typeBinExpr == typeof(string))
                res = CodeExecuteBinaryExpr<string>(left, right, be.Op);
            else if (typeBinExpr == typeof(DateTime))
                res = CodeExecuteBinaryExpr<DateTime>(left, right, be.Op);
            else
                res = CodeExecuteBinaryExpr<object>(left, right, be.Op);
            
            return res;
        }

        // TODO: Para borrar, versi�n obsoleta (pruebas).
        private object CodeExecuteBinaryExprOld(BinaryExpr be)
        {
            object res = null;
            object left = GenExpr(be.Left);
            object right = GenExpr(be.Right);

            if (left.GetType() == typeof(float) && right.GetType() == typeof(float))
                res = CodeExecuteBinaryExprFloat(left, right, be.Op);                
            else if (left.GetType() == typeof(DateTime) && right.GetType() == typeof(DateTime))
                res = CodeExecuteBinaryExprDateTime(left, right, be.Op);
            else
                res = CodeExecuteBinaryExprGeneric(left, right, be.Op);            

            // TODO: Implementar m�s operadores aqu� con tipos de datos distintos
            //       a float (string)

            return res;
        }

        private object CodeExecuteUnaryExpr(UnaryExpr ue)
        {
            object res = null;
            object ex = GenExpr(ue.Expression);

            switch (ue.Op)
            {
                case BinOp.Add:
                    if (ex.GetType() == typeof(int))
                        res = Convert.ToInt32(ex);
                    if (ex.GetType() == typeof(float))
                        res = Convert.ToSingle(ex);
                    if (ex.GetType() == typeof(double))
                        res = Convert.ToDouble(ex);
                    if (ex.GetType() == typeof(decimal))
                        res = Convert.ToDecimal(ex);

                    break;

                case BinOp.Sub:
                    if (ex.GetType() == typeof(int))
                        res = -1 * Convert.ToInt32(ex);
                    if (ex.GetType() == typeof(float))
                        res = -1 * Convert.ToSingle(ex);
                    if (ex.GetType() == typeof(double))
                        res = -1 * Convert.ToDouble(ex);
                    if (ex.GetType() == typeof(decimal))
                        res = -1 * Convert.ToDecimal(ex);

                    break;

                case BinOp.Not:
                    res = (ex.Equals(0)) ? 0 : 1;
                    break;

                default:
                    throw new ApplicationException(string.Format(
                        "The operator '{0}' is not supported in this expression ", ue.Op));
            }

            return res;                    
        }

        // TODO: Borrar, obsoleto
        private object CodeExecuteBinaryExprFloat(object left, object right, BinOp binExpOp)
        {
            object res = null;

            bool boolLeft;
            bool boolRight;

            if ((float)left != 0)
                boolLeft = true;
            else
                boolLeft = false;

            if ((float)right != 0)
                boolRight = true;
            else
                boolRight = false;

            switch (binExpOp)
            {
                case BinOp.Add:
                    res = (float)left + (float)right;
                    break;
                case BinOp.Sub:
                    res = (float)left - (float)right;
                    break;
                case BinOp.Mul:
                    res = (float)left * (float)right;
                    break;
                case BinOp.Div:
                    res = (float)left / (float)right;
                    break;
                case BinOp.Or:
                    if (boolLeft || boolRight)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.And:
                    if (boolLeft && boolRight)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.Equal:
                    if ((float)left == (float)right)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.NotEqual:
                    if ((float)left == (float)right)
                        res = 0.0f;
                    else
                        res = 1.0f;
                    break;
                case BinOp.LessThan:
                    if ((float)left < (float)right)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.LessThanOrEqual:
                    if ((float)left <= (float)right)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.GreaterThan:
                    if ((float)left > (float)right)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.GreaterThanOrEqual:
                    if ((float)left >= (float)right)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                default:
                    throw new ApplicationException(string.Format(
                        "The operator '{0}' is not supported  in this expression ", binExpOp));
            }

            return res;

        }

        private object CodeExecuteBinaryExpr<T>(object left, object right, BinOp binExpOp) 
            
        {
            object res = null;
            bool boolLeft;
            bool boolRight;

            bool notSupport = false;

            if (left.Equals(1))
                boolLeft = true;
            else
                boolLeft = false;

            if (right.Equals(1))
                boolRight = true;
            else
                boolRight = false;

            switch (binExpOp)
            {
                case BinOp.Add:
                    if (typeof(T) == typeof(int))                        
                        res = Convert.ToInt32(left) + Convert.ToInt32(right);
                    else if (typeof(T) == typeof(float))
                        res = Convert.ToSingle(left) + Convert.ToSingle(right);
                    else if (typeof(T) == typeof(double))                        
                        res = Convert.ToDouble(left) + Convert.ToDouble(right);
                    else if (typeof(T) == typeof(decimal))                        
                        res = Convert.ToDecimal(left) + Convert.ToDecimal(right);
                    else if (typeof(T) == typeof(string))
                        res = left.ToString() + right.ToString();                        
                    break;

                case BinOp.Sub:
                    if (typeof(T) == typeof(int))
                        res = Convert.ToInt32(left) - Convert.ToInt32(right);
                    else if (typeof(T) == typeof(float))
                        res = Convert.ToSingle(left) - Convert.ToSingle(right);
                    else if (typeof(T) == typeof(double))
                        res = Convert.ToDouble(left) - Convert.ToDouble(right);
                    else if (typeof(T) == typeof(decimal))
                        res = Convert.ToDecimal(left) - Convert.ToDecimal(right);
                    break;

                case BinOp.Mul:
                    if (typeof(T) == typeof(int))
                        res = Convert.ToInt32(left) * Convert.ToInt32(right);
                    else if (typeof(T) == typeof(float))
                        res = Convert.ToSingle(left) * Convert.ToSingle(right);
                    else if (typeof(T) == typeof(double))
                        res = Convert.ToDouble(left) * Convert.ToDouble(right);
                    else if (typeof(T) == typeof(decimal))
                        res = Convert.ToDecimal(left) * Convert.ToDecimal(right);
                    break;

                case BinOp.Div:
                    if (typeof(T) == typeof(int))
                        res = Convert.ToInt32(left) / Convert.ToInt32(right);
                    else if (typeof(T) == typeof(float))
                        res = Convert.ToSingle(left) / Convert.ToSingle(right);
                    else if (typeof(T) == typeof(double))
                        res = Convert.ToDouble(left) / Convert.ToDouble(right);
                    else if (typeof(T) == typeof(decimal))
                        res = Convert.ToDecimal(left) / Convert.ToDecimal(right);
                    break;

                // TODO: Pendiente, valorar si conviene que res siempre
                //       devueva un int o bool

                case BinOp.Or:
                    res = (boolLeft || boolRight) ? 1 : 0;
                    break;

                case BinOp.And:
                    res = (boolLeft && boolRight) ? 1 : 0;
                    break;

                case BinOp.Equal:
                    if (typeof(T) == typeof(int))
                        res = (Convert.ToInt32(left) == Convert.ToInt32(right)) ? 1 : 0;                    
                    else if (typeof(T) == typeof(float))
                        res = (Convert.ToSingle(left) == Convert.ToSingle(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(double))
                        res = (Convert.ToDouble(left) == Convert.ToDouble(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(decimal))
                        res = (Convert.ToDecimal(left) == Convert.ToDecimal(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(DateTime))
                        res = (Convert.ToDateTime(left) == Convert.ToDateTime(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(string))
                        res = (left.ToString() == right.ToString()) ? 1 : 0;
                    else if (typeof(T) == typeof(object))
                        res = (left.GetHashCode() == right.GetHashCode()) ? 1 : 0;
                    break;

                case BinOp.NotEqual:
                    if (typeof(T) == typeof(int))
                        res = (Convert.ToInt32(left) == Convert.ToInt32(right)) ? 0 : 1;
                    else if (typeof(T) == typeof(float))
                        res = (Convert.ToSingle(left) == Convert.ToSingle(right)) ? 0 : 1;
                    else if (typeof(T) == typeof(double))
                        res = (Convert.ToDouble(left) == Convert.ToDouble(right)) ? 0 : 1;
                    else if (typeof(T) == typeof(decimal))
                        res = (Convert.ToDecimal(left) == Convert.ToDecimal(right)) ? 0 : 1;
                    else if (typeof(T) == typeof(DateTime))
                        res = (Convert.ToDateTime(left) == Convert.ToDateTime(right)) ? 0 : 1;
                    else if (typeof(T) == typeof(string))
                        res = (left.ToString() == right.ToString()) ? 0 : 1;
                    else if (typeof(T) == typeof(object))
                        res = (left.GetHashCode() == right.GetHashCode()) ? 0 : 1;
                    break;

                case BinOp.LessThan:
                    if (typeof(T) == typeof(int))
                        res = (Convert.ToInt32(left) < Convert.ToInt32(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(float))
                        res = (Convert.ToSingle(left) < Convert.ToSingle(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(double))
                        res = (Convert.ToDouble(left) < Convert.ToDouble(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(decimal))
                        res = (Convert.ToDecimal(left) < Convert.ToDecimal(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(DateTime))
                        res = (Convert.ToDateTime(left) < Convert.ToDateTime(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(string))
                        res = (string.Compare(left.ToString(), right.ToString()) < 0) ? 1 : 0;

                    break;

                case BinOp.LessThanOrEqual:
                    if (typeof(T) == typeof(int))
                        res = (Convert.ToInt32(left) <= Convert.ToInt32(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(float))
                        res = (Convert.ToSingle(left) <= Convert.ToSingle(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(double))
                        res = (Convert.ToDouble(left) <= Convert.ToDouble(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(decimal))
                        res = (Convert.ToDecimal(left) <= Convert.ToDecimal(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(DateTime))
                        res = (Convert.ToDateTime(left) <= Convert.ToDateTime(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(string))
                        res = (string.Compare(left.ToString(), right.ToString()) < 0 || left.ToString() == right.ToString()) ? 1 : 0;

                    break;

                case BinOp.GreaterThan:
                    if (typeof(T) == typeof(int))
                        res = (Convert.ToInt32(left) > Convert.ToInt32(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(float))
                        res = (Convert.ToSingle(left) > Convert.ToSingle(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(double))
                        res = (Convert.ToDouble(left) > Convert.ToDouble(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(decimal))
                        res = (Convert.ToDecimal(left) > Convert.ToDecimal(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(DateTime))
                        res = (Convert.ToDateTime(left) > Convert.ToDateTime(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(string))
                        res = (string.Compare(left.ToString(), right.ToString()) > 0) ? 1 : 0;

                    break;

                case BinOp.GreaterThanOrEqual:
                    if (typeof(T) == typeof(int))
                        res = (Convert.ToInt32(left) >= Convert.ToInt32(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(float))
                        res = (Convert.ToSingle(left) >= Convert.ToSingle(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(double))
                        res = (Convert.ToDouble(left) >= Convert.ToDouble(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(decimal))
                        res = (Convert.ToDecimal(left) >= Convert.ToDecimal(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(DateTime))
                        res = (Convert.ToDateTime(left) >= Convert.ToDateTime(right)) ? 1 : 0;
                    else if (typeof(T) == typeof(string))
                        res = (string.Compare(left.ToString(), right.ToString()) > 0 || left.ToString() == right.ToString()) ? 1 : 0;

                    break;

                default:
                    notSupport = true;
                    break;
            }

            if (notSupport)
                throw new ApplicationException(string.Format(
                    "The operator '{0}' is not supported  in this expression ", binExpOp));
            else 
                return res;
        }

        // TODO: Borrar, obsoleto
        private object CodeExecuteBinaryExprDateTime(object left, object right, BinOp binExpOp)
        {
            object res = null;

            switch (binExpOp)
            {
                case BinOp.Equal:
                    if ((DateTime)left == (DateTime)right)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.NotEqual:
                    if ((DateTime)left == (DateTime)right)
                        res = 0.0f;
                    else
                        res = 1.0f;
                    break;
                case BinOp.LessThan:
                    if ((DateTime)left < (DateTime)right)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.LessThanOrEqual:
                    if ((DateTime)left <= (DateTime)right)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.GreaterThan:
                    if ((DateTime)left > (DateTime)right)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.GreaterThanOrEqual:
                    if ((DateTime)left >= (DateTime)right)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;

                default:
                    throw new ApplicationException(string.Format(
                        "The operator '{0}' is not supported in this expression ", binExpOp));
            }

            return res;
        }

        // TODO: Borrar, obsoleto
        private object CodeExecuteBinaryExprGeneric(object left, object right, BinOp binExpOp)
        {
            object res = null;

            switch (binExpOp)
            {
                case BinOp.Add:
                    res = left.ToString() + right.ToString();
                    break;
                case BinOp.Equal:
                    if (left.ToString() == right.ToString())
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.NotEqual:
                    if (left.ToString() == right.ToString())
                        res = 0.0f;
                    else
                        res = 1.0f;
                    break;
                case BinOp.LessThan:
                    if (string.Compare(left.ToString(), right.ToString()) < 0)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.LessThanOrEqual:
                    if (string.Compare(left.ToString(), right.ToString()) < 0 || left.ToString() == right.ToString())
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.GreaterThan:
                    if (string.Compare(left.ToString(), right.ToString()) > 0)
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                case BinOp.GreaterThanOrEqual:
                    if (string.Compare(left.ToString(), right.ToString()) > 0 || left.ToString() == right.ToString())
                        res = 1.0f;
                    else
                        res = 0.0f;
                    break;
                default:
                    throw new ApplicationException(string.Format(
                        "The operator '{0}' is not supported in this expression ", binExpOp));
            }

            return res;
        }

        private void CodeExecutePrint(Print print)
        {
            string s = GenExpr(print.Expr).ToString();
            if (s == @"\")
                textOut.AppendText("\r\n");
            else
                textOut.AppendText(@s);            
        }

        private double CodeExecuteReadNum()
        {
            // TODO: Esto es para pruebas
            double value = 0;
            InputBoxResult test = InputBox.Show(
                             "Read int  " + "\n" + " .... " +
                             "  Prompt " + "\n" + "...."
                             , "Title", "0", 100, 100);

            if (test.ReturnCode == DialogResult.OK)
                value = double.Parse(test.Text);

            return value;
        }

        #endregion

        #region Utils

        private Type TypeOfExpr(Expr expr)
        {
            if (expr is StringVal)
            {
                return typeof(string);
            }
            else if (expr is IntVal)
            {
                return typeof(int);
            }
            else if (expr is FloatVal)
            {
                return typeof(float);
            }
            else if (expr is DoubleVal)
            {
                return typeof(double);
            }
            else if (expr is DecimalVal)
            {
                return typeof(decimal);
            }
            else if (expr is DateTimeVal)
            {
                return typeof(DateTime);
            }
            else if (expr is Variable)
            {
                Variable var = (Variable)expr;
                IdentObject io = new IdentObject(var.Ident);
                if (this.symbolTable.ContainsKey(io.Obj))
                {
                    return symbolTable[io.Obj].GetType();
                }
                else
                {
                    throw new System.Exception("undeclared variable '" + var.Ident + "'");
                }
            }
            else if (expr is FunctionExpr)
            {
                // TODO: averiguar el valor de retorno de la 
                //       funci�n que viene en expr y devolver ese tipo                
                return typeof(object);
            }
            else if (expr is NewObjectExpr)
            {
                // TODO: averiguar el valor de retorno de la 
                //       objeto que viene en expr y devolver ese tipo                
                return typeof(object);
            }
            else if (expr is BinaryExpr)
            {
                return typeof(BinaryExpr);
            }
            else if (expr is UnaryExpr)
            {
                return typeof(UnaryExpr);
            }
            else
            {
                throw new System.Exception("don't know how to calculate the type of " + expr.GetType().Name);
            }
        }

        private Type TypeOfBinaryExpr(object left, object right)
        {

            // int
            if (left.GetType() == typeof(int) && right.GetType() == typeof(int))
                return typeof(int);

            // float
            else if (left.GetType() == typeof(float) && right.GetType() == typeof(float))
                return typeof(float);

            else if (  (left.GetType() == typeof(float) || right.GetType() == typeof(float)) &&
                (left.GetType() == typeof(int) || right.GetType() == typeof(int))  
                )
                return typeof(float);

            // double
            else if ( left.GetType() == typeof(double) && right.GetType() == typeof(double) )
                return typeof(double);

            else if ((left.GetType() == typeof(double) || right.GetType() == typeof(double)) &&
                (left.GetType() == typeof(int) || right.GetType() == typeof(int) || left.GetType() == typeof(float) || right.GetType() == typeof(float) )
                )
                return typeof(double);

            // decimal 
            else if (left.GetType() == typeof(decimal) && right.GetType() == typeof(decimal))
                return typeof(decimal);

            else if ((left.GetType() == typeof(decimal) || right.GetType() == typeof(decimal)) &&
                (left.GetType() == typeof(int) || right.GetType() == typeof(int) || left.GetType() == typeof(float) || right.GetType() == typeof(float) || left.GetType() == typeof(double) || right.GetType() == typeof(double) ) 
                )
                return typeof(decimal);

            // string  !!!   ||
            else if (left.GetType() == typeof(string) || right.GetType() == typeof(string))
                return typeof(string);

            // DateTime
            else if (left.GetType() == typeof(DateTime) && right.GetType() == typeof(DateTime))
                return typeof(DateTime);

            // object
            else
                return typeof(object);
            

        }

        private void SetValue(object varObj, IdentObject ident, object newValue, int i = 0)
        {
            Type t;
            PropertyInfo pi;
            string literalObjChild;
            object objChild;

            t = varObj.GetType();

            if (i < ident.ChainObjs.Count)
            {
                literalObjChild = ident.ChainObjs[i];
                pi = t.GetProperty(literalObjChild);
                objChild = pi.GetValue(varObj, null);
                i++;
                SetValue(objChild, ident, newValue, i);
            }
            else
            {
                pi = t.GetProperty(ident.Member);
                pi.SetValue(varObj, newValue, null);
            }

            return;
        }

        private void GetValue(object varObj, IdentObject ident, out object newValue, int i = 0)
        {
            Type t;
            PropertyInfo pi;
            string literalObjChild;
            object objChild;

            t = varObj.GetType();

            if (i < ident.ChainObjs.Count)
            {
                literalObjChild = ident.ChainObjs[i];
                pi = t.GetProperty(literalObjChild);
                objChild = pi.GetValue(varObj, null);
                i++;
                GetValue(objChild, ident, out newValue, i);
            }
            else
            {
                pi = t.GetProperty(ident.Member);                
                newValue = pi.GetValue(varObj, null);
            }

            return;
        }

        private void GetObjectMethod(object objRoot, IdentObject ident, out object objRet, out MethodInfo methodInfo, int i = 0)
        {
            Type t;
            PropertyInfo pi;
            string literalObjChild;
            object objChild;

            t = objRoot.GetType();

            if (i < ident.ChainObjs.Count)
            {
                literalObjChild = ident.ChainObjs[i];
                pi = t.GetProperty(literalObjChild);
                objChild = pi.GetValue(objRoot, null);
                i++;
                GetObjectMethod(objChild, ident, out objRet, out methodInfo, i);
            }
            else
            {                
                objRet = objRoot;
                methodInfo = t.GetMethod(ident.Member);
            }

            return;
        }

        #endregion

    } // CodeRun class

    #region auxiliary types

    // TODO: Se deber�a pasar al parser o al scanner
    class IdentObject
    {
        public string Obj { get; set; }
        public List<string> ChainObjs { get; set; }
        public string Member { get; set; }

        public IdentObject(string ident)
        {
            string[] tmp;

            if (string.IsNullOrEmpty(ident))
                return;

            ChainObjs = new List<string>();

            tmp = ident.Split('.');

            for(int i = 0; i < tmp.Length; i++)
            {
                if(i == 0)
                    Obj = tmp[i];
                else if (i == tmp.Length - 1)
                    Member = tmp[i];
                else
                    ChainObjs.Add(tmp[i]);                
            }            
        }
    }

    #endregion

} // namespace