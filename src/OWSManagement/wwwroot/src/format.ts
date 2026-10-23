// The database stores some times without a zone; every time the console shows is UTC.
export function toDate(value: string | null | undefined): Date | null {
	if (!value) return null;
	const iso = /[zZ]|[+-]\d\d:?\d\d$/.test(value) ? value : `${value}Z`;
	const date = new Date(iso);
	return isNaN(date.getTime()) ? null : date;
}

export function utc(value: string | null | undefined): string {
	const date = toDate(value);
	return date ? `${date.toISOString().slice(0, 16).replace('T', ' ')} UTC` : '';
}

export function ago(value: string | null | undefined, now = Date.now()): string {
	const date = toDate(value);
	if (!date) return '—';
	const seconds = Math.round((now - date.getTime()) / 1000);
	const future = seconds < 0;
	const s = Math.abs(seconds);
	const text =
		s < 45 ? `${s} s` : s < 3600 ? `${Math.round(s / 60)} min` : s < 86400 ? `${Math.round(s / 3600)} h` : `${Math.round(s / 86400)} d`;
	return future ? `in ${text}` : `${text} ago`;
}

export function num(value: number | null | undefined, digits = 0): string {
	if (value === null || value === undefined || isNaN(value)) return '—';
	return value.toLocaleString('en-US', { maximumFractionDigits: digits, minimumFractionDigits: digits });
}

export function duration(seconds: number | null | undefined): string {
	if (seconds === null || seconds === undefined) return '—';
	const h = Math.floor(seconds / 3600);
	const m = Math.floor((seconds % 3600) / 60);
	return h > 0 ? `${h} h ${m} min` : `${m} min`;
}

/** The last segment of a gameplay tag: Identification.Character.PlayerClass.Warrior -> Warrior. */
export function leaf(tag: string | null | undefined): string {
	if (!tag) return '—';
	const parts = tag.split('.');
	return parts[parts.length - 1];
}

export function itemName(tag: string): string {
	return tag.replace(/^Item\./, '');
}

/** npc_shop_buy -> NPC shop buy */
export function source(value: string | null | undefined): string {
	return (value || 'other').replace(/_/g, ' ').replace(/^npc\b/, 'NPC');
}

export const instanceStatus: Record<number, string> = { 0: 'off', 1: 'starting', 2: 'running', 3: 'stopping' };
