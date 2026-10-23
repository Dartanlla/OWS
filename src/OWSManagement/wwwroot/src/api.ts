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
	maintenance: MaintenanceWindow | null;
	rates: RateEvent | null;
	playerCap: number;
	waiting: number;
	openReports: number;
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

export interface MaintenanceWindow {
	startsAt: string;
	message: string;
}

export interface RateEvent {
	name: string;
	exp: number;
	drop: number;
	startsAt: string;
	endsAt: string;
}

export interface OpsStatus {
	maintenance: MaintenanceWindow | null;
	rates: RateEvent | null;
	playerCap: number;
	waiting: number;
}

export interface PlayerReport {
	reportID: number;
	createDate: string;
	reporterName: string;
	targetName: string;
	reason: string;
	mapName: string | null;
	mapInstanceID: number | null;
	details: string;
	chatExcerpt: string;
	resolvedAt: string | null;
	resolvedBy: string | null;
	resolutionNote: string | null;
	targetReports30d: number;
	reporterReports30d: number;
}

export interface ChatExcerptLine {
	at: string;
	channel: string;
	sender: string;
	to: string | null;
	text: string;
	dropped: boolean;
}

export interface TradePartner {
	partner: string;
	trades: number;
	juriGiven: number;
	juriReceived: number;
	itemsGiven: number;
	itemsReceived: number;
	lastTrade: string;
}

export interface TradePair {
	a: string;
	b: string;
	trades: number;
	aGaveJuri: number;
	bGaveJuri: number;
	aGaveItems: number;
	bGaveItems: number;
}

export interface DropMob {
	mob: string;
	kills: number;
}

export interface DropItem {
	mob: string;
	item: string;
	rolls: number;
	drops: number;
	chance: number;
	expectedDrops: number;
	variance: number;
	deviation: number | null;
}

export interface NewPlayerCohort {
	day: string;
	accounts: number;
	madeCharacter: number;
	level5: number;
	level10: number;
	nextDay: number;
	afterAWeek: number;
}

export interface StoppedPlayer {
	characterName: string;
	createDate: string;
	level: number;
	mapName: string | null;
	lastSeen: string | null;
	lastQuest: string | null;
	lastQuestEvent: string | null;
	minutesPlayed: number;
}

export interface QuestFunnelRow {
	quest: string;
	accepted: number;
	completed: number;
	medianMinutes: number | null;
}

export interface Outlier {
	characterName: string;
	hours: number;
	longestSessionHours: number;
	juriPerHour: number;
	levelsPerHour: number;
	lootPerHour: number;
	medianJuriPerHour: number;
	medianLevelsPerHour: number;
	medianLootPerHour: number;
	medianLongestSessionHours: number;
}

export interface LivePosition {
	characterName: string;
	mapInstanceID: number;
	mapName: string | null;
	x: number;
	y: number;
	z: number;
	updatedAt: string;
	level: number;
}

export interface RestoreSnapshot {
	characterID: number;
	charName: string;
	gold: number;
	level: number;
	jobLevel: number;
	mapName: string | null;
	x: number;
	y: number;
	z: number;
	items: { inventory: string; slot: number; item: string; quantity: number; customData: string | null }[];
	abilities: { tag: string; value: string }[];
	quests: { tag: string; value: string }[];
}

export interface StagingStatus {
	state: 'loading' | 'ready' | 'failed' | 'stopped';
	dump: string;
	at: string;
	stopsAt: string;
	error: string;
}

/** Parses a JSON column the API returns as text; the fallback when it is empty or not JSON. */
export function parseJson<T>(text: string | null | undefined, fallback: T): T {
	try {
		return text ? (JSON.parse(text) as T) : fallback;
	} catch {
		return fallback;
	}
}
