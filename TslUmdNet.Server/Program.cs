using Microsoft.AspNetCore.Mvc;
using TslUmdNet;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();



//http://localhost/api/tally/192.168.11.1/8900/0/1/3
app.MapGet("/tally/{ip}/{port}/{screen}/{index}/{tallyValue}", (string ip, int port, int screen, int index, int tallyValue) =>
{
    TSL5 tsl = new TSL5();

    TallyData td = new TallyData();
    td.Screen = (short)screen;
    td.Index = (short)index;    
    td.Display.RhTally = (byte)tallyValue;
    td.Display.TextTally = 0;
    td.Display.LhTally = 0;
    td.Display.Brightness = 3;
    td.Display.Text = "Test Tally";

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

