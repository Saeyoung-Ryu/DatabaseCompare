using DBcompare.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MatBlazor;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using System;
using System.IO;
using DBcompare.Manager;
using Syncfusion.Blazor;

var builder = WebApplication.CreateBuilder(args);

RefreshManager.RefreshAll();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMatBlazor();
builder.Services.AddServerSideBlazor();
builder.Services.AddSyncfusionBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

{
   

}


app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Console.WriteLine("Database Compare Tool Started!!");
app.Run();