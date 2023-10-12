using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TAG.Networking.OpenAI.Messages;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Operators.Conditional;
using Waher.Security;
using Waher.Things;
using Waher.Things.Metering;

namespace TAG.Things.OpenAI.ScriptExtensions
{
	/// <summary>
	/// Provides access to Open AI ChatGPT chat completion API.
	/// </summary>
	public class ChatGpt : FunctionMultiVariate
	{
		/// <summary>
		/// Provides access to Open AI ChatGPT chat completion API.
		/// </summary>
		/// <param name="Arg1">Argument 1</param>
		/// <param name="Arg2">Argument 2</param>
		/// <param name="Arg3">Argument 3</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGpt(ScriptNode Arg1, ScriptNode Arg2, ScriptNode Arg3,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Arg1, Arg2, Arg3 }, argumentTypes3Normal, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Provides access to Open AI ChatGPT chat completion API.
		/// </summary>
		/// <param name="Arg1">Argument 1</param>
		/// <param name="Arg2">Argument 2</param>
		/// <param name="Arg3">Argument 3</param>
		/// <param name="Arg4">Argument 4</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGpt(ScriptNode Arg1, ScriptNode Arg2, ScriptNode Arg3, ScriptNode Arg4,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Arg1, Arg2, Arg3, Arg4 }, argumentTypes4Normal, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Provides access to Open AI ChatGPT chat completion API.
		/// </summary>
		/// <param name="Arg1">Argument 1</param>
		/// <param name="Arg2">Argument 2</param>
		/// <param name="Arg3">Argument 3</param>
		/// <param name="Arg4">Argument 4</param>
		/// <param name="Arg5">Argument 5</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGpt(ScriptNode Arg1, ScriptNode Arg2, ScriptNode Arg3, ScriptNode Arg4, ScriptNode Arg5,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Arg1, Arg2, Arg3, Arg4, Arg5 }, argumentTypes5Normal, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Provides access to Open AI ChatGPT chat completion API.
		/// </summary>
		/// <param name="Arg1">Argument 1</param>
		/// <param name="Arg2">Argument 2</param>
		/// <param name="Arg3">Argument 3</param>
		/// <param name="Arg4">Argument 4</param>
		/// <param name="Arg5">Argument 5</param>
		/// <param name="Arg6">Argument 6</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGpt(ScriptNode Arg1, ScriptNode Arg2, ScriptNode Arg3, ScriptNode Arg4, ScriptNode Arg5, ScriptNode Arg6,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6 }, argumentTypes6Normal, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(ChatGpt);

		/// <summary>
		/// Default argument names.
		/// </summary>
		public override string[] DefaultArgumentNames => new string[] { "Instruction", "Sender", "Text", "Functions", "History", "Preview" };

		/// <summary>
		/// If the node (or its decendants) include asynchronous evaluation. Asynchronous nodes should be evaluated using
		/// <see cref="EvaluateAsync(Variables)"/>.
		/// </summary>
		public override bool IsAsynchronous => true;

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// This method should be used for nodes whose <see cref="IsAsynchronous"/> is false.
		/// </summary>
		/// <param name="Arguments">Arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
		{
			return this.EvaluateAsync(Arguments, Variables).Result;
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// This method should be used for nodes whose <see cref="IsAsynchronous"/> is false.
		/// </summary>
		/// <param name="Arguments">Arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override async Task<IElement> EvaluateAsync(IElement[] Arguments, Variables Variables)
		{
			if (!Waher.IoTGateway.ScriptExtensions.Functions.GetNode.TryGetDataSource(MeteringTopology.SourceID, out IDataSource Source))
				throw new ScriptRuntimeException(MeteringTopology.SourceID + " data source not found.", this);

			INode Node = await Source.GetNodeAsync(new ThingReference("ChatGPT", MeteringTopology.SourceID))
				?? throw new ScriptRuntimeException("ChatGPT node not found in data source " + MeteringTopology.SourceID + ".", this);

			if (!(Node is ChatGPTXmppBridge ChatGpt))
				throw new ScriptRuntimeException("ChatGPT node not of expected type.", this);

			if (string.IsNullOrEmpty(ChatGpt.ApiKey))
				throw new ScriptRuntimeException("ChatGPT node not configured with API key.", this);

			int i = 0;
			int c = Arguments.Length;
			object Obj;
			string Sender = null;
			string Text;
			bool History = false;
			bool Preview = false;
			List<Networking.OpenAI.Functions.Function> Functions = null;

			Obj = Arguments[i++].AssociatedObjectValue;
			if (!(Obj is string Instruction))
				throw new ScriptRuntimeException("Invalid arguments.", this);

			Obj = Arguments[i++].AssociatedObjectValue;
			if (!(Obj is string s1))
				throw new ScriptRuntimeException("Invalid arguments.", this);

			Obj = Arguments[i++].AssociatedObjectValue;
			if (Obj is bool b1)
			{
				Text = s1;
				History = b1;

				if (i < c)
					Preview = If.ToBoolean(Arguments[i++]) ?? throw new ScriptRuntimeException("Invalid arguments.", this);
			}
			else if (Obj is string s2)
			{
				Sender = s1;
				Text = s2;

				Obj = Arguments[i++].AssociatedObjectValue;

				if (Obj is bool b2)
				{
					History = b2;

					if (i < c)
						Preview = If.ToBoolean(Arguments[i++]) ?? throw new ScriptRuntimeException("Invalid arguments.", this);
				}
				else if (Obj is Networking.OpenAI.Functions.Function F)
				{
					Functions = new List<Networking.OpenAI.Functions.Function>() { F };

					if (i < c)
					{
						History = If.ToBoolean(Arguments[i++]) ?? throw new ScriptRuntimeException("Invalid arguments.", this);

						if (i < c)
							Preview = If.ToBoolean(Arguments[i++]) ?? throw new ScriptRuntimeException("Invalid arguments.", this);
					}
				}
				else if (Obj is Array A)
				{
					Functions = new List<Networking.OpenAI.Functions.Function>();

					foreach (object Item in A)
					{
						if (!(Item is Networking.OpenAI.Functions.Function F2))
							throw new ScriptRuntimeException("Invalid function definition.", this);

						Functions.Add(F2);
					}

					if (i < c)
					{
						History = If.ToBoolean(Arguments[i++]) ?? throw new ScriptRuntimeException("Invalid arguments.", this);

						if (i < c)
							Preview = If.ToBoolean(Arguments[i++]) ?? throw new ScriptRuntimeException("Invalid arguments.", this);
					}
				}
				else
					throw new ScriptRuntimeException("Invalid arguments.", this);
			}
			else if (Obj is Networking.OpenAI.Functions.Function F)
			{
				Text = s1;
				Functions = new List<Networking.OpenAI.Functions.Function>() { F };

				if (i < c)
					Preview = If.ToBoolean(Arguments[i++]) ?? throw new ScriptRuntimeException("Invalid arguments.", this);
			}
			else if (Obj is Array A)
			{
				Text = s1;

				Functions = new List<Networking.OpenAI.Functions.Function>();

				foreach (object Item in A)
				{
					if (!(Item is Networking.OpenAI.Functions.Function F2))
						throw new ScriptRuntimeException("Invalid function definition.", this);

					Functions.Add(F2);
				}

				if (i < c)
				{
					History = If.ToBoolean(Arguments[i++]) ?? throw new ScriptRuntimeException("Invalid arguments.", this);

					if (i < c)
						Preview = If.ToBoolean(Arguments[i++]) ?? throw new ScriptRuntimeException("Invalid arguments.", this);
				}
			}
			else
				throw new ScriptRuntimeException("Invalid arguments.", this);

			if (string.IsNullOrEmpty(Sender))
			{
				if (Variables.TryGetVariable("QuickLoginUser", out Variable v) && v.ValueObject is IUser User)
					Sender = GetSender(User);
				else if (Variables.TryGetVariable("User", out Variable v2) && v2.ValueObject is IUser User2)
					Sender = GetSender(User2);
				else
					throw new ScriptRuntimeException("Unable to determine sender.", this);

				if (string.IsNullOrEmpty(Sender))
					throw new ScriptRuntimeException("Unable to determine sender.", this);
			}

			Message Response;

			if (Preview)
			{
				Response = await ChatGpt.ChatQueryWithHistory(Sender, Text, Instruction, Functions?.ToArray(),
					!History, (sender, e) =>
					{
						if (Preview && Variables.HandlesPreview && !string.IsNullOrEmpty(e.Total))
							Variables.Preview(this.Expression, new StringValue(e.Total));

						return Task.CompletedTask;
					}, null);
			}
			else
				Response = await ChatGpt.ChatQueryWithHistory(Sender, Text, Instruction, Functions?.ToArray(), !History, null, null);

			Dictionary<string, object> Result = new Dictionary<string, object>()
			{
				{ "Content", Response.Content }
			};

			if (!string.IsNullOrEmpty(Response.FunctionName))
			{
				Dictionary<string, object> Function = new Dictionary<string, object>()
				{
					{ "Name", Response.FunctionName },
					{ "Arguments", Response.FunctionArguments }
				};

				Result["Function"] = Function;

				foreach (Networking.OpenAI.Functions.Function F in Functions)
				{
					if (F.Name == Response.FunctionName)
					{
						if (Variables.TryGetVariable(Response.FunctionName, out Variable v) &&
							v.ValueObject is ILambdaExpression Lambda)
						{
							Function["Result"] = await EvaluateLambda(Lambda, Response.FunctionArguments, Variables);
							break;
						}
						else if (Variables.TryGetVariable(Response.FunctionName + " " + F.Parameters.Length.ToString(), out v) &&
							v.ValueObject is ILambdaExpression Lambda2)
						{
							Function["Result"] = await EvaluateLambda(Lambda2, Response.FunctionArguments, Variables);
							break;
						}
					}
				}
			}

			return new ObjectValue(Result);
		}

		private static async Task<IElement> EvaluateLambda(ILambdaExpression Lambda, Dictionary<string, object> Arguments, Variables Variables)
		{
			try
			{
				int i, c = Lambda.NrArguments;
				IElement[] Args = new IElement[Lambda.NrArguments];
				string[] Names = Lambda.ArgumentNames;

				for (i = 0; i < c; i++)
				{
					if (Arguments.TryGetValue(Names[i], out object Obj))
						Args[i] = Expression.Encapsulate(Obj);
					else
						Args[i] = ObjectValue.Null;
				}

				if (Lambda.IsAsynchronous)
					return await Lambda.EvaluateAsync(Args, Variables);
				else
					return Lambda.Evaluate(Args, Variables);
			}
			catch (Exception ex)
			{
				return new ObjectValue(ex);
			}
		}

		private static string GetSender(IUser User)
		{
			if (User is null)
				return null;

			Type T = User.GetType();
			PropertyInfo PI = T.GetRuntimeProperty("Jid");
			if (!(PI is null))
			{
				if (PI.GetValue(User) is string Jid && !string.IsNullOrEmpty(Jid))
					return Jid;
			}

			return User.UserName;
		}
	}
}
