import { Story } from './story.model';
export interface Feature {
    name: string;
    stories: { [sprintId: string]: Story[] };
    parkingLot: Story[];
}