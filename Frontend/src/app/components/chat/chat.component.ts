import {
  AfterViewChecked,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  signal,
  ViewChild,
} from '@angular/core';
import { ApiService } from '../../services/api.service';
import { ChatMessage } from '../../models/chat.model';
import { MarkdownPipe } from './markdown.pipe';

interface UiMessage {
  role: 'user' | 'assistant';
  content: string;
  pending?: boolean;
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [MarkdownPipe],
  templateUrl: './chat.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatComponent implements AfterViewChecked {
  @ViewChild('messagesEnd') private messagesEnd!: ElementRef;

  private readonly api = inject(ApiService);

  protected readonly messages = signal<UiMessage[]>([
    {
      role: 'assistant',
      content:
        'Olá! Sou seu assistente de negócios. Tenho acesso ao catálogo atual dos seus fornecedores. Posso ajudar com:\n\n' +
        '- **Melhores preços** por modelo e fornecedor\n' +
        '- **Sugestões de precificação** com margem personalizada\n' +
        '- **Rascunhos de mensagens** para seus clientes\n' +
        '- **Análise de fornecedores** e confiabilidade\n\n' +
        'O que você precisa?',
    },
  ]);

  protected readonly input = signal('');
  protected readonly isSending = signal(false);

  private get history(): ChatMessage[] {
    return this.messages()
      .filter(m => !m.pending)
      .map(m => ({ role: m.role, content: m.content }));
  }

  protected send(): void {
    const text = this.input().trim();
    if (!text || this.isSending()) return;

    this.input.set('');
    this.isSending.set(true);

    this.messages.update(msgs => [
      ...msgs,
      { role: 'user', content: text },
      { role: 'assistant', content: '', pending: true },
    ]);

    const historySnapshot = this.history.slice(0, -1); // exclude pending

    this.api.chat(text, historySnapshot).subscribe({
      next: (reply) => {
        this.messages.update(msgs =>
          msgs.map(m => m.pending ? { role: 'assistant', content: reply } : m)
        );
        this.isSending.set(false);
      },
      error: () => {
        this.messages.update(msgs =>
          msgs.map(m => m.pending
            ? { role: 'assistant', content: '⚠ Erro ao se comunicar com a IA. Tente novamente.' }
            : m)
        );
        this.isSending.set(false);
      },
    });
  }

  protected onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  protected clear(): void {
    this.messages.set([
      {
        role: 'assistant',
        content: 'Conversa reiniciada. Como posso ajudar?',
      },
    ]);
  }

  ngAfterViewChecked(): void {
    this.messagesEnd?.nativeElement.scrollIntoView({ behavior: 'smooth' });
  }
}
