# FusionChat

![image](https://user-images.githubusercontent.com/58620778/191639427-75e8086b-be4c-44e9-bfa9-c5ed87910723.png)


FusionChat은 [Stl.Fusion](https://github.com/servicetitan/Stl.Fusion) 라이브러리를 이용해서 어떤 애플리케이션을 만들 수 있는지 소개하는데 목적이 있습니다.

그 예시로 Stl.Fusion을 이용해서 채팅 프로그램을 간단히 만들었습니다.


## 개요

먼저 Stl.Fusion을 이용하면 다음의 이점이 있습니다.

- 실시간 기능의 구현 간소화
- 자동 캐시 적용

실시간 기능과 캐시 기능은 컴퓨팅 퓨전의 서비스(Compute Service)를 통해 제공됩니다. Stl.Fusion의 컴퓨팅 서비스란 상태가 변경되지 않았을 경우 캐싱된 값을 이용해서 빠르게 원하는 정보를 획득할 수 있고 상태가 무효화 되었을 경우 이 값을 참조하고 있는 (더 정확한 표현은 컴퓨팅 메서드를 사용하고 있는) 곳에서 변경되었음을 쉽게 확인하도록 해서 실시간 앱을 쉽게 만들 수 있도록 합니다.


퓨전은 이러한 컴퓨팅 서비스를 API로 제공하기 위해 그대로 웹API를 사용합니다. 즉, 굳이 상태 변경 통지를 받고 싶지 않다면 기존 우리가 알고 있는 웹API를 사용하는 방법처럼 그대로 사용이 가능합니다.

![image](https://user-images.githubusercontent.com/58620778/191649454-a23c0107-1eb2-4a69-86a5-271da5e180ed.png)

`swagger.json`을 제공하므로 swagger의 클라이언트 API 생성기를 이용해 API 호출 코드를 쉽게 생성해 사용할 수 있습니다.

또한 [Stl.Fusion.Client](https://www-0.nuget.org/packages/Stl.Fusion.Client) 패키지를 이용하면 몇줄의 설정 코드와 인터페이스만 정의하는것으로 클라이언트에서 쉽게 퓨전으로 만들어진 컴퓨팅 서비스를 이용하고 또 상태가 변경되었을 때 그 통지를 받을 수 있습니다.


## 채팅 기능

구현된 채팅 기능은 퓨전 기능만을 빠르게 보여주는 목적으로 인증 및 권한 기능은 생략했습니다.

채팅 기능은 크게 두가지 API로 구성됩니다.

- GetChatMessages(index)
   - 인덱스 위치 이후의 채팅 메시지를 가져옵니다.
- SendMessage(message);
   - 채팅 메시지를 전송합니다.

이 기능은 [FusionChatServer.Services.ChatService.cs](https://github.com/dimohy/FusionChat/blob/master/FusionChatServer/Services/ChatService.cs)에 구현되어 있습니다. 일단은 이 기능이 어떻게 실시간 기능이 되는지는 넘어갑시다. 소스코드를 보면 `List<ChatMessage>`를 이용해 매우 간단한 로직으로 구현된 것을 확인할 수 있습니다.

이 기능은 [FusionChatServer.Controllers.ChatController.cs](https://github.com/dimohy/FusionChat/blob/master/FusionChatServer/Controllers/ChatController.cs)에 의해 웹API로 노출됩니다. 퓨전의 여러 데코레이션(특성)과 특징을 제외하면 여러분도 쉽게 이해할 수 있는 일반적인 웹API 형태입니다. 그런데 어떻게 이 코드가 실시간 서비스가 될까요?


## 컴퓨팅 서비스 (Compute Service)

퓨전의 컴퓨팅 서비스는 이미 연산이 되어 다시 연산할 필요가 없는 정보를 캐싱 합니다.

### 컴퓨팅 메서드

```csharp
[ComputeMethod]
public virtual async Task<ChatInfo> GetChatMessages(int index, CancellationToken cancellationToken = default)
{
    await EveryChatTail();

    ChatInfo chatInfo;
    lock (_chatInfosLock)
    {
        chatInfo = new ChatInfo(_chatInfos.Count, _chatInfos.Skip(index).ToArray());
    }

    return chatInfo;
}
```

`GetChatMMessages()`는 컴퓨팅 메서드입니다. index값이 동일하다면, `메서드가 한번만 호출`되고 그 다음부터는 메서드가 실행되지 않고 이미 반환된 ChatInfo가 퓨전에 의해 `캐싱` 되어 즉각적으로 반환됩니다. 이렇게 동작하게 만드는 데코레이션은 `[ComputeMethod]` 특성과 `virtual` 키워드입니다.

> `await EveryChatTail()` 는 다른 컴퓨팅 메서드와의 종속 관계를 만들어줍니다.

이 코드의 동작을 더욱 극적으로 확인하기 위해서 위 코드 블럭에 `await Task.Dealy(1000)`을 줘서 1초 동안의 처리로 시뮬레이션 하고 그 동작성을 확인할 수 있습니다.

> 최초 한번만 1초의 시간이 걸리고 이후 부터는 캐싱된 값이 즉각적으로 반환됩니다.


### 컴퓨팅 값의 무효화

위의 `GetChatMessages()`의 결과가 캐싱되는 생명주기는 반드시 `SendMessage()`가 호출되었을 끝나야 합니다. 그렇죠? 새로운 메시지가 추가되었으니까요. 이것을 다음의 코드를 통해 살펴볼 수 있습니다.


```csharp
public Task SendMessage(ChatMessage message, CancellationToken cancellationToken = default)
{
    lock (_chatInfosLock)
    {
        _chatInfos.Add(message);
    }

    using (Computed.Invalidate())
    {
        _ = EveryChatTail();
    }

    return Task.CompletedTask;
}
```

이렇게 `using (Computed.Invalidate()) { }` 블럭에 표현할 수 있습니다. 그런데 원래는 `_ = EveryCheckTail()`이 아니라 `_ = GetChatMessages(...)`로 표현되어야 했었습니다. 무슨 말이냐고요?

퓨전의 컴퓨팅 메서드와 그 값의 무효화의 기본적인 구조는 다음과 같습니다.

```csharp
[ComputeMethod]
public virtual async Task<Result> GetComputedValue()
{
   var result = await LongLongComputing();
   return result;
}

public Task ResetComputedValue()
{
    using (Computed.Invalidate())
    {
        _ = GetComputedValue();
    }
}
```

`using (Computed.Invalidate()) { }` 블럭의 `_ = GetComputedValue()`에 의해 `GetComputedValue()`에 의해 계산된 값이 무효화 되고 다시 `GetComputedValue()`를 호출할 때 캐싱된 값을 사용하지 않고 다시 메서드 함수가 실행됩니다.

그러데 다음의 코드처럼 재계산이 될 필요가 없는 경우도 있겠죠. 피보나치 수열의 각 값은 한번 계산되면 변하지 않습니다.

```csharp
[ComputeMethod]
public virtual async Task<int> GetFibonacciNumber(int n)
{
   return Task.FromResult(fib(n));

   int fib(int n)
   {
      if (n < 2)
         return n;
      else
         return fib(n - 1) + fib(n - 2);
    }
   }
}
```

하지만 `GetChatMessages(index)`는 메시지가 추가될 때마다 그 결과가 달라져야 하므로 (추가된 메시지도 반환되어야 하므로) 무효화 되어야 하는데 퓨전은,

**퓨전 메서드의 매개인자 기준으로 캐싱** 하므로 `_ = GetChatMessages(...)`으로는 무효화 할 수가 없는 것이죠.


### 컴퓨팅 메서드의 무효화 트리거

컴퓨팅 메서드 끼리 의존성이 있다면 퓨전은 영향받는 컴퓨팅 메서드 까지도 자동으로 무효화 해줍니다. 똑똑합니다! 다시 코드를 봅시다.

```csharp
[ComputeMethod]
public virtual async Task<ChatInfo> GetChatMessages(int index, CancellationToken cancellationToken = default)
{
    await EveryChatTail();

    ChatInfo chatInfo;
    lock (_chatInfosLock)
    {
        chatInfo = new ChatInfo(_chatInfos.Count, _chatInfos.Skip(index).ToArray());
    }

    return chatInfo;
}

[ComputeMethod]
protected virtual Task<Unit> EveryChatTail() => TaskExt.UnitTask;

public Task SendMessage(ChatMessage message, CancellationToken cancellationToken = default)
{
    lock (_chatInfosLock)
    {
        _chatInfos.Add(message);
    }

    using (Computed.Invalidate())
    {
        _ = EveryChatTail();
    }

    return Task.CompletedTask;
}
```

`index`에 상관없이 `GetChatMessages()`의 캐싱된 전체 값을 무효화 하기 위한 트릭으로 아무것도 계산하지 않는 `EveryChatTail()`을 만들고, `SendMessage()`에서 `EveryChatTail()` 으로 캐싱된 값을 무효화 한 후 `GetChatMessages()`에서 단지 한번 `await EveryChatTail()`로 호출해줌으로써 의존성을 만들었습니다.

| 의존성 관계
EveryChatTail() <- GetChatMessages()

즉, `EveryChatTail()`의 값이 무효화 되면 `index`와 상관없이 `GetChatMessages()`의 캐신된 값도 무효화 됩니다.


자, 여기까지는 컴퓨팅 서비스에 관한 이야기였습니다. 하지만 실시간 앱은 실시간으로 반응해야 하니까요. 어떻게 실시간으로 반응해 관련 처리를 할 있는지 살펴봅시다.


## 반응형 서비스

`FusionChatServer`에서 제공하는 웹API만 가지고서는 실시간 서비스를 제공할 수 없습니다. 똑똑하게도 퓨전은 약간의 조정 만으로 실시간 서비스를 제공하도록 만들 수 있습니다.


### 첫번째 조정 `Publish` 특성

[FusionChatServer.Controllers.ChatController.cs](https://github.com/dimohy/FusionChat/blob/master/FusionChatServer/Controllers/ChatController.cs) 코드를 보면 `GetMessage()`에 `[Publish]` 특성이 있는 것을 알 수 있습니다. 이 특성으로 인해 퓨전이 해당 API가 무효화 될 때마다 클라이언트로 알려줘야 하는지를 알게 됩니다.

이제 `GetChatMessages()`의 값이 무효화 될 때마다 클라이언트에서 수신할 수 있어요! 하지만 이를 이용하려면 `Stl.Fusion.Client` 패키지를 이용해야 하는데 [FusionChat.FusionChatClient.cs](https://github.com/dimohy/FusionChat/blob/master/FusionChat/FusionChatClient.cs)의 코드처럼 만들고 사용할 수 있습니다.

`Publish`로 무효화 통지를 받게 되었으므로 이를 감지하려면, 해당 API 메서드를 한번만 호출하고 아래의 코드처럼 무효화 되었을 때 처리하는 로직을 만들 수 있습니다.

[FusionChat.MainForms.cs](https://github.com/dimohy/FusionChat/blob/master/FusionChat/MainForm.cs)

```csharp
...
var client = new FusionChatClient(new Uri("https://localhost:7233/"));
_computedState = client.StateFactory.NewComputed<ChatInfo>(new ComputedState<ChatInfo>.Options()
{
    UpdateDelayer = FixedDelayer.Instant
}, async (state, CancellationToken) =>
{
    var result = await client.ChatService.GetChatMessages(messageIndex);
    messageIndex = result.TotalMessages;

    this.BeginInvoke(() =>
    {
        foreach (var message in result.Messages)
        {
            chatText.AppendText($"{message.Nickname}: {message.Message}\r\n");
        }
    });

    return result;
});
...
```

> 기본 설정으로인해 `NewComputed()`으로 등록된 콜백 함수가 한번 호출됩니다.


이제 되었습니다! 여러개의 폼을 띄워 놓고 어떤에서든지 메시지를 보냈을 때 퓨전은 `GetChatMessages()` 의 값을 무효화 하고 `Publish` 특성에 의해 클라이언트에 다시 `GetChatMessages()`의 값이 변경되었음을 통보하여 `NewComputed()`로 등록한 콜백 함수가 호출되고, 이곳에서 해당 정보를 업데이트 하게 되면 실시간 채팅 앱이 완성되었습니다! ^^

클라이언트에서의 컴퓨팅 메서드의 사용은 퓨전의 복제 서비스(Replica Service)에 의해 클라이언트에서도 캐싱이 되어 편안하게 마구마구 API를 호출해도 최적의 속도로 동작합니다.


## 정리

FusionChat은 [Stl.Fusion](https://github.com/servicetitan/Stl.Fusion)의 강력한 기능을 소개하기 위한 목적으로 만들어졌으므로 복제 서비스(Replica Service) 및 각종 옵션에 대한 설명을 생략하였습니다. 좀 더 자세한 내용은 제가 번역하고 있는 [Fusion 튜토리얼](https://dimohy.slogger.today/fusion) 및 [원문](https://github.com/servicetitan/Stl.Fusion.Samples/tree/master/docs/tutorial)을 참고하시기 바랍니다.


