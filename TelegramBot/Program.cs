using JFA.Telegram.Console;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var users = new List<User>();
var rooms = new List<Room>();

var botManager = new TelegramBotManager();
var bot = botManager.Create("6017973886:AAGi0pU6xyPzswKt8G3-6pG8jBKLnTq0M4U");
botManager.Start(NewMessage);

var botDetails = await bot.GetMeAsync();
Console.WriteLine(botDetails.FirstName + " is working");

botManager.Start(NewMessage);

void NewMessage(Update update)
{
    if (update.Type != UpdateType.Message)
        return;

    var message = update.Message!.Text;

    var user = CheckUser(update);

    switch (user.NextMessage)
    {
        case ENextMessage.Created: SendEnterName(user); break;
        case ENextMessage.Name: SaveNameAndSendMenu(user, message); break;
        case ENextMessage.Menu: ChooseMenu(user, message); break;
        case ENextMessage.RoomName: CreateNewRoom(user, message); break;
        case ENextMessage.RoomMenu: ChooseRoomMenu(user, message); break;
        case ENextMessage.OutlayName: SaveOutlayAndSendPrice(user, message); break;
        case ENextMessage.OutlayPrice: SaveOutlayPrice(user, message); break;
        case ENextMessage.RoomKey: JoinRoomWithKey(user, message); break;
    }

    Console.WriteLine(update.Message!.Text);
}

User CheckUser(Update update)
{
    var chatId = update.Message!.From!.Id;

    User? user = users.FirstOrDefault(u => u.ChatId == chatId);

    if (user == null)
    {
        user = new User();
        user.ChatId = chatId;
        users.Add(user);
    }

    return user;
}



void SendEnterName(User user)
{
    bot.SendTextMessageAsync(user.ChatId, "Enter your name?");
    user.NextMessage = ENextMessage.Name;
}

void SaveNameAndSendMenu(User user, string message)
{
    user.Name = message;
    user.NextMessage = ENextMessage.Menu;

    SendMenu(user);
}

void SendMenu(User user)
{
    var menuText = "Menu:\nCreate room\nJoin room";

    bot.SendTextMessageAsync(user.ChatId, menuText);
}

void ChooseMenu(User user, string message)
{
    switch (message)
    {
        case "Create room": CreateRoom(user); break;
        case "Join room": JoinRoom(user); break;
    }
}

void CreateRoom(User user)
{
    bot.SendTextMessageAsync(user.ChatId, "Enter room name?");
    user.NextMessage = ENextMessage.RoomName;
}

void JoinRoom(User user)
{
    bot.SendTextMessageAsync(user.ChatId, "Enter room key to join?");
    user.NextMessage = ENextMessage.RoomKey;
}

void CreateNewRoom(User user, string messsage)
{
    var room = new Room
    {
        OvnerChatId = user.ChatId,
        Name = messsage,
        UsersId = new List<long>() { user.ChatId },
        Outlays = new List<Outlay>(),
        Key = Guid.NewGuid().ToString("N")
    };

    rooms.Add(room);

    SendRoomMenu(user, room);
}


void SendRoomMenu(User user, Room room)
{
    var keyboard = new ReplyKeyboardMarkup(new List<List<KeyboardButton>>()
    {
        new List<KeyboardButton>()
        {
            new KeyboardButton("AddOutlay")
        },
        new List<KeyboardButton>()
        {
            new KeyboardButton("Outlays")
        },
        new List<KeyboardButton>()
        {
            new KeyboardButton("Room Details")
        },
        new List<KeyboardButton>()
        {
            new KeyboardButton("Exit")
        }
    });

     bot.SendTextMessageAsync(user.ChatId, $"Room{room.Name}", replyMarkup: keyboard);
    user.CurrentRoom = room;
    user.NextMessage = ENextMessage.RoomMenu;
}

void ChooseRoomMenu(User user, string message)
{
    switch (message)
    {
        case "AddOutlay": AddOutlay(user); break;
        case "Outlays": SendOutlay(user); break;
        case "Room Details": SendRoomDetails(user); break;
        case "Exit": Exit(user); break;
    }
}

void SendOutlay(User user)
{
    var outlays = "Outlays\n";

    var rows = new List<List<InlineKeyboardButton>>();

    foreach (var outlay in user.CurrentRoom.Outlays)
    {
        var productNameButton = InlineKeyboardButton.WithCallbackData(outlay.ProductName);
        var productPriceButton = InlineKeyboardButton.WithCallbackData(outlay.Price.ToString());
        var productOwnerButton = InlineKeyboardButton.WithCallbackData(outlay.UserChatId.ToString());
        rows.Add(new List<InlineKeyboardButton>() { productOwnerButton, productNameButton, productPriceButton });
    }

    var keyboard = new InlineKeyboardMarkup(rows);

    bot.SendTextMessageAsync(user.ChatId, outlays, replyMarkup: keyboard);
}

void AddOutlay(User user)
{
    bot.SendTextMessageAsync(user.ChatId, "Enter outlay name?");
    user.NextMessage = ENextMessage.OutlayName;
}

void SaveOutlayAndSendPrice(User user, string message)
{
    var outlay = new Outlay
    {
        UserChatId = user.ChatId,
        ProductName = message,
        Date = DateTime.Now
    };

    user.CurrentRoom.Outlays.Add(outlay);
    user.CurrentAddingOutlay = outlay;

    bot.SendTextMessageAsync(user.ChatId, "Enter outlay price?");
    user.NextMessage = ENextMessage.OutlayPrice;
}

void SaveOutlayPrice(User user, string message)
{
    var price = Convert.ToInt64(message);

    user.CurrentAddingOutlay.Price = price;

    user.NextMessage = ENextMessage.RoomMenu;
    SendRoomMenu(user, user.CurrentRoom);
}

void SendRoomDetails(User user)
{
    _ = bot.SendTextMessageAsync(user.ChatId, $"RoomKey:{user.CurrentRoom.Key}\nUsers: {user.CurrentRoom.UsersId.Count}");
}

void JoinRoomWithKey(User user, string message)
{
    var room = rooms.FirstOrDefault(r => r.Key == message);
    if (room == null)
    {
        bot.SendTextMessageAsync(user.ChatId, "Invalid key! try again");
    }
    else
    {
        user.CurrentRoom = room;
        room.UsersId.Add(user.ChatId);
        SendRoomMenu(user, user.CurrentRoom);
    }
}

void Exit(User user)
{
    user.CurrentRoom = null;
    user.NextMessage = ENextMessage.Menu;
    SendMenu(user);
}







