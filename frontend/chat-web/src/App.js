import React, { useState, useEffect } from 'react';
import './App.css';

// Backend URL'iniz (launchSettings.json dosyanizdaki HTTP portu: 5136)
// Bu URL'i kendi localhost adresinizle degistirdiginizden emin olun.
const API_URL = 'http://localhost:5136/api/messages';

function App() {
    const [messages, setMessages] = useState([]);
    const [nickname, setNickname] = useState('');
    const [messageText, setMessageText] = useState('');

    // 1. Mesajlari Yükleme (GET) - Uygulama ilk açıldığında çalışır
    useEffect(() => {
        fetchMessages();
    }, []);

    const fetchMessages = async () => {
        try {
            const response = await fetch(API_URL);
            const data = await response.json();
            setMessages(data);
        } catch (error) {
            console.error("Mesajlar yuklenirken hata:", error);
        }
    };

    // 2. Mesaj Gönderme (POST)
    const sendMessage = async () => {
        if (!nickname || !messageText) return alert("Rumuz ve mesaj gerekli!");

        const newMessage = {
            // Backend modelinizdeki alan adlari: Name, Description
            name: nickname,
            description: messageText,
            // Backendde AI tarafından doldurulacak alanlar
            feeling: "string",
            score: 0
        };

        try {
            const response = await fetch(API_URL, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(newMessage)
            });

            if (response.ok) {
                // Mesaj gonderimi basarili oldu, listeyi guncelle
                fetchMessages();
                setMessageText(''); // Inputu temizle
            } else {
                console.error("Mesaj gonderilemedi.");
            }
        } catch (error) {
            console.error("Mesaj gonderimi sirasinda hata:", error);
        }
    };

    return (
        <div className="App">
            <h1>FullStack Chat + AI Analiz 💬</h1>

            {/* Mesaj Listesi */}
            <div className="chat-container">
                {messages.map((msg, index) => (
                    <div key={index} className="message">
                        <strong>{msg.name}:</strong> {msg.description}

                        {/* AI Sonucunu Gösterme */}
                        <span className={`sentiment ${msg.feeling?.toLowerCase()}`}>
                            ({msg.feeling} - {msg.score?.toFixed(2)})
                        </span>
                        <small> - {new Date(msg.timestamp).toLocaleTimeString()}</small>
                    </div>
                ))}
            </div>

            {/* Mesaj Gönderme Formu */}
            <div className="input-area">
                <input
                    type="text"
                    placeholder="Rumuzunuz"
                    value={nickname}
                    onChange={(e) => setNickname(e.target.value)}
                />
                <input
                    type="text"
                    placeholder="Mesajınızı yazın..."
                    value={messageText}
                    onChange={(e) => setMessageText(e.target.value)}
                />
                <button onClick={sendMessage}>Gönder</button>
            </div>

            {/* Basit CSS stillerini ekleyebilirsiniz */}
            <style>{`
        .chat-container { height: 300px; overflow-y: scroll; border: 1px solid #ccc; padding: 10px; margin-bottom: 20px; }
        .message { margin-bottom: 8px; padding: 5px; border-bottom: 1px dotted #eee; }
        .input-area input { margin-right: 10px; padding: 8px; }
        .sentiment { font-weight: bold; margin-left: 10px; }
        /* Backend'den gelen etiketlere gore renkler */
        .hata { color: red; }
        .positive { color: green; }
        .negative { color: darkred; }
        .nötür { color: orange; }
      `}</style>
        </div>
    );
}

export default App;