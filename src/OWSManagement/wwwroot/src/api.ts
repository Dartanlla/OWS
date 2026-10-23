import axios, { AxiosError } from 'axios';
import { reactive } from 'vue';

// Every write carries X-Samsara-Console (the server refuses API writes without it); the session is a cookie.
export const http = axios.create({
	baseURL: '/api',
	timeout: 30000,
	headers: { 'X-Samsara-Console': '1' },
	withCredentials: true,
});

export interface StaffUser {
	name: string;
	role: 'Moderator' | 'GameMaster' | 'Admin' | string;
	steamId: string;
}

export const session = reactive<{ user: StaffUser | null; checked: boolean }>({ user: null, checked: false });

const ranks: Record<string, number> = { Player: 0, Moderator: 1, GameMaster: 2, Admin: 3 };
export function can(role: 'Moderator' | 'GameMaster' | 'Admin'): boolean {
	return (ranks[session.user?.role ?? 'Player'] ?? 0) >= ranks[role];
}

http.interceptors.response.use(
	(response) => response,
	(error: AxiosError) => {
		if (error.response?.status === 401) session.user = null;
		return Promise.reject(error);
	},
);

export async function loadSession(): Promise<void> {
	try {
		session.user = (await http.get<StaffUser>('/auth/me')).data;
	} catch {
		session.user = null;
	}
	session.checked = true;
}

export async function signOut(): Promise<void> {
	await http.post('/auth/logout');
	session.user = null;
}

/** The message a failed call should show: the server's own refusal when it gave one. */
export function errorText(error: unknown): string {
	const response = (error as AxiosError<{ errorMessage?: string }>)?.response;
	if (response?.status === 403) return 'Your role cannot do this.';
	if (response?.data && typeof response.data === 'object' && response.data.errorMessage) return response.data.errorMessage;
	if (response) return `The server answered ${response.status}.`;
	return 'The console cannot reach its server.';
}

export interface Result {
	success: boolean;
	errorMessage?: string;
	reasonCode?: string;
}

export interface ZoneInstance {
	mapInstanceID: number;
	worldServerID: number;
	serverRegion: string;
	zoneName: string;
	port: number;
	status: number;
	numberOfReportedPlayers: number;
	lastUpdateFromServer: string | null;
	serverFrameMsAvg: number | null;
	serverFrameMsMax: number | null;
	serverCpuPct: number | null;
	memoryUsedMB: number | null;
	uptimeSeconds: number | null;
	buildCL: number | null;
	onlineCharacters: string | null;
}

export interface Signal {
	kind: 'save_failed' | 'rejected' | 'juri' | 'zone_exit' | 'duplicate';
	subject: string;
	detail: string;
	count: number;
	lastSeen: string | null;
}

export interface Overview {
	overview: {
		onlineNow: number;
		zonesUp: number;
		accountsToday: number;
		newCharactersToday: number;
		medianSessionMinutes7d: number | null;
		signals: Signal[];
	};
	instances: ZoneInstance[];
	loginsOpen: boolean;
	motd: string;
}

export interface AccountRow {
	userGUID: string;
	firstName: string;
	email: string;
	steamId: string | null;
	role: string;
	preferredServerRegion: string | null;
	createDate: string;
	lastAccess: string;
	characterCount: number;
	banned: boolean;
	muted: boolean;
}

export interface CharacterRow {
	characterID: number;
	charName: string;
	userGUID: string | null;
	accountName: string | null;
	classTag: string | null;
	baseLevel: number | null;
	jobLevel: number | null;
	mapName: string | null;
	gold: number;
	lastActivity: string;
	createDate: string;
	onlineMapInstanceID: number | null;
	onlineZone: string | null;
}

export interface Item {
	inSlotNumber: number;
	itemIDTag: string;
	quantity: number;
	instanceId: string | null;
	customData: string | null;
	equipped: boolean;
	slot: number;
	refine: number;
	stars: number;
	locked: boolean;
	damaged: boolean;
	cards: string[];
}

export interface Shop {
	playerShopID: number;
	ownerCharacterID: number;
	ownerCharName: string;
	shopName: string;
	zoneName: string;
	saleStatus: string;
	settlementStatus: string;
	proceedsGoldPending: number;
	listings: number;
	createdAt: string;
	closedAt: string | null;
}

export interface CharacterDetail extends CharacterRow {
	x: number;
	y: number;
	z: number;
	gender: number;
	isAdmin: boolean;
	isModerator: boolean;
	isInternalNetworkTestUser: boolean;
	economyRevision: number;
	role: string | null;
	stats: Record<string, number>;
	items: Item[];
	quests: { questIDTag: string; questJournalTagContainer: string }[];
	shops: Shop[];
}

export interface Sanction {
	sanctionID: number;
	userGUID: string;
	kind: 'ban' | 'mute' | 'warn' | 'note';
	reason: string;
	characterName: string | null;
	createDate: string;
	expiresAt: string | null;
	issuedBy: string;
	issuedFrom: string;
	revokedAt: string | null;
	revokedBy: string | null;
	active: boolean;
	accountName: string | null;
}

export interface AuditEntry {
	auditLogID: number;
	createDate: string;
	source: string;
	actor: string;
	action: string;
	target: string | null;
	detail: string | null;
	clientIP: string | null;
}

export interface ZoneCommand {
	commandID: number;
	mapInstanceID: number | null;
	characterName: string | null;
	command: string;
	args: string;
	createDate: string;
	createdBy: string;
	expiresAt: string;
	deliveredAt: string | null;
	completedAt: string | null;
	result: string | null;
	resultDetail: string | null;
}

export interface GameEvent {
	gameEventID: number;
	eventTime: string;
	mapInstanceID: number | null;
	mapName: string | null;
	characterName: string | null;
	event: string;
	fields: string;
}

export interface ChatLine {
	chatLogID: number;
	createDate: string;
	channel: string;
	senderName: string;
	targetName: string | null;
	message: string;
	dropped: boolean;
}
