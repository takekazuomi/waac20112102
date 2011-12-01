# Windows Azure Advent Calendar 2011/12/02

うっかり二日目担当になってしまい。最初別ネタを書こうと思ったのです
が、思ったより再現させるのが難しく。途中で断念しこのネタにしました。

TableServiceContextを使わずにAzure Tableのクエリ結果を取得する方法
を書きます。TableServiceContextはそのほとんどの処理を
DataServiceClinet に回していて、自前で実装を持っているのは、
SaveChanges のリトライ処理程度です。DataServiceClient は結構汎用に
書かれているようで、Azure Tableには過剰スペックですし、せっかくの
スキーマーレスの特徴を生かし切っていません。

Azure Table とのやり取りは、REST＋Atom なので、素のXMLを受け取って
自前で処理することもできます。今のDataServiceClientのモデルではク
エリ結果を複数のクラスのインスタンスにしようとすると難しい事になっ
てしましますが、自前でXMLからObjectを作るよういするれば異なったク
ラスのオブジェクトに戻すこともできます。

このあたりをやりたいとしばらく思っているのですが、全く進みません。

とりあえず、今回はHttpRequestを飛ばしてAtoを受けとるところまでの手
順を説明します。


## Windows Azure Tabale Storage へのローベルなアクセス

WindowsAzure.StorageClient.Library を経由して使うと結構複雑に見え
ますが、クライアント側でやってることはそんなに難しくありません。

基本的には、URLを組み立ててサーバーに投げるだけです（RESTなので）。
中でも参照は非常に簡単でURLの組立もあまりパターンがありません。

少し面倒なことは、シグニチャーを付けなければならない部分ぐらいです。

## Microsoft.WindowsAzure.CloudStorageAccount

Microsoft.WindowsAzure.CloudStorageAccount のTableEndpoint プロパ
ティにサービスのエンドポイントがあるので、それにテーブル名をつけて、
最後にQueryをつけるとURLが完成します。

そのURLから、HttpWebRequestを作って、CloudStorageAccountの
SignRequestLite を使ってシグニチャーを作ります。

このリクエストを使うとTable Storage Service からレスポンスが帰ってきます。


コードだと、こんな感じになります。


``` C#

    partial class Program
    {
        static readonly ILog logger = LogManager.GetCurrentClassLogger();

        private StreamReader GetTableStream(CloudStorageAccount account, string tableName, string query)
        {
            var requestUri = new StringBuilder();

            requestUri.AppendFormat("{0}{1}()", account.TableEndpoint.ToString(), tableName);
            if (!String.IsNullOrWhiteSpace(query))
            {
                requestUri.AppendFormat("?{0}", query);
            }

            // create Http Request
            var request = (HttpWebRequest)HttpWebRequest.Create(requestUri.ToString());

            // signs request using the specified credentials under the Shared Key Lite authentication
            account.Credentials.SignRequestLite(request);

            var response = (HttpWebResponse)request.GetResponse();

            var sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(response.CharacterSet));

            return sr;
        }

``` 


リクエストとレスポンスを、fiddler でみると下記のようになっているのが見えます。


* リクエスト


``` 

GET http://waac20111202001.table.core.windows.net/EntityOne()?$filter=(PartitionKey%20eq%20'p0000')%20and%20(RowKey%20eq%20'r0000') HTTP/1.1
x-ms-date: Thu, 01 Dec 2011 17:22:55 GMT
Authorization: SharedKeyLite waac20111202001:0+571tS5QYeavUsaP4XwaIRWgEi5iNuZF26MB0sQj40=
Host: waac20111202001.table.core.windows.net
Connection: Keep-Alive


``` 

* レスポンス


``` 

HTTP/1.1 200 OK
Cache-Control: no-cache
Transfer-Encoding: chunked
Content-Type: application/atom+xml;charset=utf-8
Server: Table Service Version 1.0 Microsoft-HTTPAPI/2.0
x-ms-request-id: 7acf2dbb-d621-4d8c-9506-f68c572585d1
Date: Thu, 01 Dec 2011 17:22:48 GMT

53B
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<feed xml:base="http://waac20111202001.table.core.windows.net/" xmlns:d="http://schemas.microsoft.com/ado/2007/08/dataservices" xmlns:m="http://schemas.microsoft.com/ado/2007/08/dataservices/metadata" xmlns="http://www.w3.org/2005/Atom">
  <title type="text">EntityOne</title>
  <id>http://waac20111202001.table.core.windows.net/EntityOne</id>
  <updated>2011-12-01T17:22:49Z</updated>
  <link rel="self" title="EntityOne" href="EntityOne" />
  <entry m:etag="W/&quot;datetime'2011-12-01T15%3A16%3A00.3401314Z'&quot;">
    <id>http://waac20111202001.table.core.windows.net/EntityOne(PartitionKey='p0000',RowKey='r0000')</id>
    <title type="text"></title>
    <updated>2011-12-01T17:22:49Z</updated>
    <author>
      <name />
    </author>
    <link rel="edit" title="EntityOne" href="EntityOne(PartitionKey='p0000',RowKey='r0000')" />
    <category term="waac20111202001.EntityOne" scheme="http://schemas.microsoft.com/ado/2007/08/dataservices/scheme" />
    <content type="application/xml">
      <m:properties>
        <d:PartitionKey>p0000</d:PartitionKey>
        <d:RowKey>r0000</d:RowKey>
        <d:Timestamp m:type="Edm.DateTime">2011-12-01T15:16:00.3401314Z</d:Timestamp>
        <d:Data>dummy</d:Data>
      </m:properties>
    </content>
  </entry>
</feed>
0


## まとめ

こんな感じで、普通の HttpWebRequest を飛ばすのとほとんどかわらずに、
Azure Tableにアクセスできます。余計な処理もないので軽いものです。

バッチ処理などで、大量のデータを扱う場合なるべくオブジェクトの生成
を減らしたい場合は、このレイヤーまで下りて行って専用のフレームワー
クを作るのは有効な気がします。

このコードは同期処理になっていますが、効率を重視するなら非同期がお
すすめです。


だいぶ時間をオーバーしてしましたが、Windows Azure Advent Calendar
2011/12/02 を終わります。


githubの レポジトリーにはソースが入ってます。

