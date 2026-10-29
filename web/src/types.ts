export interface DrawnCard {
  deck: string
  id: string
  title: string
  summary: string
  kind: string | null
  costCents: number | null
  symbol: string | null
  pricePerShareCents: number | null
  dividendPerShareCents: number | null
  downPaymentCents: number | null
  loanCents: number | null
  monthlyCashFlowCents: number | null
  assetName: string | null
  perShare: boolean | null
  priceCents: number | null
  cashDeltaCents: number | null
}

export interface TurnRoll {
  faces: number[]
  total: number
  fromIndex: number
  toIndex: number
  paydayCashCents: number
}

export interface StockLot {
  symbol: string
  shares: number
  costPerShareCents: number
  dividendPerShareCents: number
}

export interface RealEstateLot {
  name: string
  costCents: number
  mortgageCents: number
  monthlyCashFlowCents: number
}

export interface BusinessLot {
  name: string
  costCents: number
  liabilityCents: number
  monthlyCashFlowCents: number
}

export interface LoanLine {
  name: string
  balanceCents: number
  monthlyPaymentCents: number
}

export interface PlayerState {
  name: string
  professionId: string
  professionTitle: string
  tokenColour: string
  cashCents: number
  childCount: number
  totalAssetsCents: number
  totalLiabilitiesCents: number
  salaryCents: number
  passiveIncomeCents: number
  totalIncomeCents: number
  totalExpensesCents: number
  monthlyCashFlowCents: number
  passiveIncomeExceedsExpenses: boolean
  boardIndex: number
  spaceKind: string
  spaceTitle: string
  skipTurnsRemaining: number
  extraDieTurnsRemaining: number
  stocks: StockLot[]
  realEstate: RealEstateLot[]
  businesses: BusinessLot[]
  track: string
  isBankrupt: boolean
  loans: LoanLine[]
  isClone: boolean
}

export interface FastTrackDeal {
  id: string
  title: string
  summary: string
  costCents: number
  monthlyCashFlowCents: number
}

export interface GameState {
  id: string
  phase: string
  turnPhase: string
  currentTurnIndex: number
  lastRoll: TurnRoll | null
  pendingDeck: string | null
  drawn: DrawnCard | null
  players: PlayerState[]
  winnerIndex: number | null
  fastTrackDeals: FastTrackDeal[]
  seed: number | null
}

export interface SeatRequest {
  name: string
  professionId: string
  tokenColour: string
}
