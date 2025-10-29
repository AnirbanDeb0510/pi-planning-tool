import { Story } from './story.model';

export interface Sprint {
  id: string;
  name: string;
  stories: Story[];
}
