using Microsoft.AspNetCore.Mvc;
using TslUmdNet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();


app.MapGet("/tally/{ip}/{port}/{screen}/{index}/{tallyValue}", (string ip, int port, int screen, int index, byte tallyValue) =>
{
    TSL5 tsl = new TSL5();

    TallyData td = new TallyData(screen, index, tallyValue);
    

    try
    {
        tsl.SendTallyTCP(ip, port, td);
        tsl.SendTallyUDP(ip, port, td);
    }
    catch (Exception e)
    {
        return Results.BadRequest(e.Message);
    }

    return Results.Ok(td);
});


app.Run();

