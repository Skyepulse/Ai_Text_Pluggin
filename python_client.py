import socket
from summarizer import Summarizer #BERT

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


class question_summarizer:
    
    def __init__(self):
        self.client = client_socket('127.0.0.1', 8052)

        

        self.received_string = ""
        self.summarized_string = ""

        print("Summarizer initializing...")
        self.summarizer = Summarizer()
        print("Summarizer initialized.")

        #Handshake with server
        self.client.send_message("Summarizer ready")
        print(self.client.receive_message())
        self.start()

    def summarize(self, text:str) -> str:
        print("Summarizing text")
        summarized = self.summarizer(text, min_length=50, max_length=150)
        print("Summarized text: ", summarized)
        return summarized
    
    def divide_text(self, text:str) -> (str, str): # type: ignore
        #The text received is in this format: "Question: <question> Your response: <response>\n"
        #We need to divide it into the question and the response
        if "Question:" not in text or "Your answer:" not in text:
            return "", ""
        question = ""
        response = ""
        response_index = text.find("Your answer: ")
        question = text[len("Question: "):response_index].strip()
        response = text[response_index + len("Your answer: "):].strip()
        response = response.rstrip("\n")
        print("Question: ", question)
        print("Answer: ", response)
        return question, response

        
    def start(self):
        #The Summarizer is a client that receives a string from the server and then summarizes it before sending it back to the server
        while True:
            print("Waiting for message from server")
            self.received_string = self.client.receive_message()
            print("Received message: ", self.received_string)
            if(self.received_string == "exit\n"):
                print("Communication ended by server")
                break
            question, response = self.divide_text(self.received_string)
            if not question == "" and not response == "":
                
                summarized_response = self.summarize(response)
                summarized_question = self.summarize(question)
                to_send = f"Question: {summarized_question} Your response: {summarized_response}"
                success = self.client.send_message(to_send)
                if not success:
                    print("Error sending message, terminating communication.")
                    break
            else:
                print("Error dividing text, waiting for next.")
        self.client.s.close()

if __name__ == "__main__":
    summarizer = question_summarizer()