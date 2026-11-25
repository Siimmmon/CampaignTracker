export interface Campaign {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  events: CampaignEvent[];
}

export interface CampaignEvent {
  id: number;
  eventType: string;
  timestamp: string;
  campaignId: number;
}

export interface CreateCampaign {
  name: string;
  startDate: string;
  endDate: string;
  events?: Pick<CampaignEvent, 'eventType' | 'timestamp'>[];
}

export function parseCampaignId(value: string): number | null {
  const id = Number(value);
  return /^\d+$/.test(value) && id >= 1 && id <= 2_147_483_647 ? id : null;
}
