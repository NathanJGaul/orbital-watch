// Module-level variable = NOT localStorage or sessionStorage
// THis is intentional: localStorage is readable by any script on the page (XSS risk)
// The token is lost on page refresh, which is accepted trade-off for this for this dashboard.
// A production app would pair this with an HttpOnly refresh-token cookie.

// A module-level variable is accessable anywhere
// not react component dependent, no prop drilling or state management required
let currentToken: string | null = null;

export function setToken(token: string) {
    currentToken = token;
}

export function getToken(): string | null {
    return currentToken;
}

export function clearToken()
{
    currentToken = null;
}

export async function login(userId: string): Promise<string> {
    const response = await fetch(`/api/auth/token`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ userId }),
    });
    
    if (!response.ok) {
        throw new Error('Failed to login');
    }
    
    const data = await response.json();
    setToken(data.token);
    return data.token;
}