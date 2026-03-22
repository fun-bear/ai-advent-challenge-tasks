using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace AIAdventChallenge.ViewModels;

public readonly record struct AgentPowerParam(AgentPower Value)
{
    public static ValueTask<AgentPowerParam> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var raw = context.Request.Query["power"].ToString();
        var parsed = Enum.TryParse<AgentPower>(raw, ignoreCase: true, out var power)
            ? power
            : AgentPower.Medium;

        return ValueTask.FromResult(new AgentPowerParam(parsed));
    }

    public static implicit operator AgentPower(AgentPowerParam param) => param.Value;
}