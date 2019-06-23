# Ancorazor

Ancorazor ��һ������ .NET Core 2.2 �� Angular 7 �ļ��򲩿�ϵͳ��

[Demo](https://siegrain.wang)

_��Ŀ��Ȼ�ڿ����У����һ�û������̨������ǰ̨Ҳ�л����Ĺ����ܣ�������ǿ���õĽ׶Ρ�_

## ������Ŀ

### ��������

ȷ�����Ļ����Ѿ�����Щ�����ˣ�

1. .NET Core 2.2 SDK
2. Nodejs 10+
3. SQL Server(�� docker-compose ���Բ������)

### ��������

1. `git clone https://github.com/siegrainwong/ancorazor.git`
2. �滻`ancorazor/Ancorazor.API/appsettings.Development.json`�е������ַ���(��ѡ��ȡ�����㱾�ص� SQL Server ���ã�һ�㲻��Ҫ�滻)
3. �� `cd path-to-ancorazor/Ancorazor.API` ����Ŀ¼��ִ�� `dotnet watch run`
4. �� `localhost:8088`, Ĭ���û������� admin/123456.

### docker-compose ����

`cd path-to-ancorazor/build`

#### windows

����`dev.ps1`����������`F:\Projects\ancorazor\`·���ַ����滻����ģ�Ȼ����������ű�

#### linux

���� `path-to-ancorazor/build/dev.sh`

docker-compose �Ὣ sql server��skywalking��nginx �� ancorazor һ��������

- Skywalking: `localhost:8080`, Ĭ���û������� is admin/admin.
- Ancorazor: `localhost:8088`, Ĭ���û������� is admin/123456.

## ����(CI/CD)

�Ƽ��ο����� azure devops �ϵ�[pipeline](https://dev.azure.com/siegrainwong/Ancorazor/_build?definitionId=6)�����ǹ����ġ�

## ��Ŀ�ṹ

TODO

## To-do

- [x] Comment
- [ ] Management page
- [ ] Search
- [ ] Categories & tags page
- [ ] Tests

��ο� [project](https://github.com/Seanwong933/ancorazor/projects/1).

## ��л

[ģ��: startbootstrap-clean-blog](https://github.com/BlackrockDigital/startbootstrap-clean-blog)

## Licence

Anti-996 & MIT
