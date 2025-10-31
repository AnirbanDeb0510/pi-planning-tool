export interface Story {
  id: string;
  title: string;
  feature: string;
  points?: number;     // legacy single total
  devPoints?: number;  // optional
  testPoints?: number; // optional
}
