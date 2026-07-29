export default {
  name: 'ChatPage',

  data() {
    return {
      title: 'Direct Messages (Vue SPA)',
      myNickname: 'VueUser_' + Math.floor(Math.random() * 899 + 100),
      activeRecipient: 'Everyone',
      suggestedContacts: ['DevUser', 'Alex', 'Jordan', 'Support'],
      newMessage: '',
      messages: [],
      isConnected: false,
      hubConnection: null
    };
  },

  computed: {
    filteredMessages() {
      return this.messages.filter(m => 
        this.activeRecipient === 'Everyone' || 
        m.recipient === 'Everyone' ||
        m.recipient === this.myNickname ||
        m.sender === this.myNickname ||
        m.sender === this.activeRecipient
      );
    }
  },

  methods: {
    formatTime(ts) {
      if (!ts) return '';
      const d = new Date(ts);
      return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    },

    async initSignalR() {
      if (typeof signalR === 'undefined') {
        console.warn('SignalR client SDK not found on window object.');
        return;
      }

      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl('/chathub')
        .withAutomaticReconnect()
        .build();

      this.hubConnection.on('ReceiveChatMessage', (dto) => {
        this.messages.push(dto);
        this.scrollToBottom();
      });

      try {
        await this.hubConnection.start();
        this.isConnected = true;
      } catch (err) {
        console.error('Vue SPA SignalR connection error:', err);
        setTimeout(() => this.initSignalR(), 3000);
      }
    },

    async sendMessage() {
      if (!this.newMessage.trim() || !this.isConnected) return;

      const text = this.newMessage;
      this.newMessage = '';

      try {
        await this.hubConnection.invoke('SendMessage', this.myNickname, this.activeRecipient, text);
      } catch (err) {
        console.error('Failed to send chat message:', err);
      }
    },

    scrollToBottom() {
      this.$nextTick(() => {
        const el = document.getElementById('vue-chat-thread');
        if (el) el.scrollTop = el.scrollHeight;
      });
    }
  },

  mounted() {
    this.initSignalR();
  },

  beforeDestroy() {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  },

  template: /*html*/ `
  <v-container fluid class="py-4">
    <v-row justify="center">
      <v-col cols="12" md="11" lg="10">
        
        <v-card class="elevation-6 rounded-lg overflow-hidden">
          <v-toolbar color="primary" dark flat>
            <v-icon left>mdi-forum</v-icon>
            <v-toolbar-title class="font-weight-bold">Direct Messages (Vue SPA)</v-toolbar-title>
            <v-spacer></v-spacer>
            <v-chip color="green accent-3" text-color="black" small class="font-weight-bold" v-if="isConnected">
              <v-icon left small>mdi-circle</v-icon> Live Connected
            </v-chip>
            <v-chip color="warning" small class="font-weight-bold" v-else>
              <v-icon left small>mdi-loading mdi-spin</v-icon> Connecting...
            </v-chip>
          </v-toolbar>

          <v-row no-gutters style="height: 600px;">
            
            <!-- Left Contact / Channel List -->
            <v-col cols="12" sm="4" md="3" class="grey lighten-4 border-r d-flex flex-column h-100">
              <div class="p-3 border-b bg-white pa-3">
                <v-text-field
                  v-model="myNickname"
                  label="Your Handle"
                  prefix="@"
                  dense
                  outlined
                  hide-details
                  class="font-weight-bold"
                ></v-text-field>
              </div>

              <div class="px-3 py-2 grey lighten-3 text-caption font-weight-bold text-uppercase grey--text text--darken-1">
                Direct Channels
              </div>

              <v-list class="transparent flex-grow-1 overflow-y-auto pa-0">
                <v-list-item
                  :input-value="activeRecipient === 'Everyone'"
                  color="primary"
                  @click="activeRecipient = 'Everyone'"
                >
                  <v-list-item-avatar color="primary" class="white--text font-weight-bold">
                    <v-icon dark>mdi-earth</v-icon>
                  </v-list-item-avatar>
                  <v-list-item-content>
                    <v-list-item-title class="font-weight-bold"># Everyone</v-list-item-title>
                    <v-list-item-subtitle>Global channel</v-list-item-subtitle>
                  </v-list-item-content>
                </v-list-item>

                <v-divider></v-divider>

                <v-list-item
                  v-for="user in suggestedContacts"
                  :key="user"
                  :input-value="activeRecipient === user"
                  color="primary"
                  @click="activeRecipient = user"
                >
                  <v-list-item-avatar color="blue-grey lighten-2" class="white--text font-weight-bold">
                    {{ user.substring(0, 2).toUpperCase() }}
                  </v-list-item-avatar>
                  <v-list-item-content>
                    <v-list-item-title class="font-weight-bold">@{{ user }}</v-list-item-title>
                    <v-list-item-subtitle>Direct message</v-list-item-subtitle>
                  </v-list-item-content>
                </v-list-item>
              </v-list>
            </v-col>

            <!-- Right Message Thread -->
            <v-col cols="12" sm="8" md="9" class="d-flex flex-column h-100 bg-white">
              
              <!-- Recipient Bar -->
              <div class="pa-3 border-b d-flex align-center justify-space-between bg-white">
                <div class="d-flex align-center">
                  <v-avatar color="primary" size="36" class="white--text font-weight-bold mr-3">
                    {{ activeRecipient === 'Everyone' ? '🌎' : activeRecipient.substring(0, 2).toUpperCase() }}
                  </v-avatar>
                  <div>
                    <div class="subtitle-1 font-weight-bold">
                      {{ activeRecipient === 'Everyone' ? 'Global Channel (#Everyone)' : '@' + activeRecipient }}
                    </div>
                    <div class="caption green--text text--darken-1">
                      <v-icon small color="green" class="mr-1">mdi-circle-small</v-icon>SignalR Channel
                    </div>
                  </div>
                </div>

                <v-btn icon small @click="messages = []" title="Clear thread">
                  <v-icon>mdi-delete-outline</v-icon>
                </v-btn>
              </div>

              <!-- Message Stream -->
              <div id="vue-chat-thread" class="flex-grow-1 pa-4 overflow-y-auto grey lighten-5">
                <div v-if="filteredMessages.length === 0" class="text-center py-10 grey--text">
                  <v-icon size="64" color="grey lighten-1">mdi-forum-outline</v-icon>
                  <div class="title mt-2">No messages in thread</div>
                  <div class="caption">Send a message to start real-time chatting!</div>
                </div>

                <div
                  v-for="(msg, idx) in filteredMessages"
                  :key="idx"
                  class="d-flex flex-column mb-3"
                  :class="msg.sender === myNickname ? 'align-end' : 'align-start'"
                >
                  <div class="caption grey--text text--darken-1 mb-1 px-1">
                    <span :class="msg.sender === myNickname ? 'primary--text font-weight-bold' : 'font-weight-medium'">
                      {{ msg.sender === myNickname ? 'You' : '@' + msg.sender }}
                    </span>
                    &bull; {{ formatTime(msg.timestamp) }}
                  </div>

                  <v-sheet
                    elevation="1"
                    rounded="lg"
                    class="pa-3 max-w-75"
                    :color="msg.sender === myNickname ? 'primary' : 'white'"
                    :class="msg.sender === myNickname ? 'white--text' : 'black--text border'"
                    style="max-width: 75%; word-break: break-word;"
                  >
                    {{ msg.message }}
                  </v-sheet>
                </div>
              </div>

              <!-- Input Bar -->
              <div class="pa-3 border-t bg-white">
                <v-form @submit.prevent="sendMessage" class="d-flex align-center">
                  <v-text-field
                    v-model="newMessage"
                    placeholder="Type a message..."
                    outlined
                    dense
                    hide-details
                    :disabled="!isConnected"
                    @keyup.enter="sendMessage"
                  ></v-text-field>
                  <v-btn
                    color="primary"
                    class="ml-2 px-6"
                    height="40"
                    :disabled="!isConnected || !newMessage.trim()"
                    @click="sendMessage"
                  >
                    <v-icon left>mdi-send</v-icon> Send
                  </v-btn>
                </v-form>
              </div>

            </v-col>

          </v-row>
        </v-card>

      </v-col>
    </v-row>
  </v-container>
  `
};
