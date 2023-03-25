// ������� ����������� ���������� ���-����������
using LABA3API;

var webAppBuilder = WebApplication.CreateBuilder(args);

// ��������� ������ ������������ � ��������� ��������� ������������.
webAppBuilder.Services.AddControllers();

// ��������� ������ Swagger � ���������.
webAppBuilder.Services.AddSwaggerGen();
webAppBuilder.Services.AddEndpointsApiExplorer();

// ������� ���������� ���-����������
var webApp = webAppBuilder.Build();

// ����������� ����� HTTP ��������.
if (webApp.Environment.IsDevelopment())
{
    // �������� Swagger ��� ����� ����������.
    webApp.UseSwagger();
    webApp.UseSwaggerUI();
}

webApp.UseHttpsRedirection();

// ���������� ������������� ���� �����������.
webApp.UseAuthorization();

// ���������� ���������� ���������.
webApp.MapControllers();

//������� ������ ������������� �� �������
JobCreator.CreateJob();
// ��������� ���-����������.
webApp.Run();

