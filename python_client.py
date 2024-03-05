import socket

class client_socket:

    def __init__(self, host:str, port:int):
        self.host = host
        self.port = port
        self.s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.s.connect((self.host, self.port))

    def send_message(self, message) -> bool:
        message += "\n"
        try:
            self.s.sendall(message.encode())
            return True
        except Exception as e:
            print("Error sending message: ", e)
            return False

    def receive_message(self) -> str:
        data = self.s.recv(1024)
        return data.decode()


class Summarizer:
    
    def __init__(self):
        self.client = client_socket('127.0.0.1', 8052)

        #Handshake with server
        self.client.send_message("Summarizer ready")
        print(self.client.receive_message())

        self.received_string = ""
        self.summarized_string = ""
        self.start()

    def summarize(self, text:str) -> str:
        if(len(text) < 10):
            self.summarized_string = text
            return self.summarized_string
        else:
            self.summarized_string = text[0:10]
            return self.summarized_string
        
    def start(self):
        #The Summarizer is a client that receives a string from the server and then summarizes it before sending it back to the server
        while True:
            self.received_string = self.client.receive_message()
            if(self.received_string == "exit\n"):
                print("Communication ended by server")
                break
            to_send = self.summarize(self.received_string)
            success = self.client.send_message(to_send)
            if not success:
                print("Error sending message, terminating communication.")
                break
        self.client.s.close()

if __name__ == "__main__":
    summarizer = Summarizer()