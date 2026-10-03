import React, { useState, useMemo, useEffect } from 'react';

/**
 * TypeScript Data Models for Electricity & Energy Monitoring Dashboard
 */

export interface DailyEnergyRecord {
  date: string; // "YYYY-MM-DD"
  dayOfWeek: string; // "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"
  dayLabel: string; // "Sat 19", "Sun 20"
  formattedDate: string; // "Sat, Sep 19"
  shortDate: string; // "Sep 19"
  monthKey: string; // "2026-09"
  monthLabel: string; // "September 2026"
  gridConnectedHours: number; // 0.0 - 24.0
  gridOutageHours: number; // 0.0 - 24.0
  gridAvailabilityPercentage: number; // 0.0 - 100.0%
  gridConsumptionKwh: number;
  solarConsumptionKwh: number;
  totalConsumptionKwh: number; // gridConsumptionKwh + solarConsumptionKwh
  solarGenerationKwh?: number;
  isRealData?: boolean;
}

export interface WeeklyGaugeMetrics {
  totalGridHours7d: number;
  availabilityPct: number;
  totalOutageHours7d: number;
}

export interface MonthlyOverviewMetrics {
  totalGridKwh: number;
  totalSolarKwh: number;
  totalKwh: number;
  avgDailyKwh: number;
  solarSharePct: number;
  gridSharePct: number;
  daysCount: number;
}

export interface WeeklyAvailabilityGaugeProps {
  gridHours7d: number;
  availabilityPct: number;
  isLight?: boolean;
}

export interface SolarDashboardProps {
  initialMonth?: string;
  autoFetchLive?: boolean;
  dataset?: DailyEnergyRecord[];
  onMonthChange?: (monthKey: string) => void;
}

// Backward compatibility interfaces
export interface HourlyPointDto {
  hour: number;
  gridPowerWatts: number;
  pvPowerWatts: number;
  loadPowerWatts: number;
  isGridAvailable: boolean;
}

export interface DailyHourlyMetricDto {
  deviceId: string;
  date: string;
  dayLabel: string;
  formattedDate: string;
  dayOfWeek: string;
  peakGridPowerWatts: number;
  totalGridEnergyKwh: number;
  gridUptimeHours: number;
  gridOutageHours: number;
  gridAvailabilityPercentage: number;
  hours: HourlyPointDto[];
}

export interface InverterReading {
  id: number;
  deviceId: string;
  timestamp: string;
  gridPowerWatts: number;
  loadPowerWatts: number;
  pvPowerWatts: number;
  acInputVoltage: number;
  isGridAvailable: boolean;
  batterySoc: number | null;
  batteryVoltage: number | null;
  operatingMode: string | null;
  createdAtUtc: string;
}

export interface DailyEnergyMetric {
  id: number;
  deviceId: string;
  date: string;
  totalLoadEnergyKwh: number;
  gridImportEnergyKwh: number;
  pvGenerationEnergyKwh: number;
  gridUptimeHours: number;
  gridOutageHours: number;
  gridAvailabilityPercentage: number;
}

// Live-only dataset: all metrics are retrieved from the live database.
export const RAW_ENERGY_DATASET: DailyEnergyRecord[] = [];

export function WeeklyAvailabilityGauge({ gridHours7d, availabilityPct, isLight = false }: WeeklyAvailabilityGaugeProps) {
  const radius = 68;
  const strokeWidth = 12;
  const center = 85;
  const totalArc = 260;
  const circumference = 2 * Math.PI * radius;
  const arcLength = (totalArc / 360) * circumference;
  const progressLength = (Math.min(100, Math.max(0, availabilityPct)) / 100) * arcLength;

  const gaugeColor = availabilityPct >= 80 ? '#10B981' : availabilityPct >= 65 ? '#F59E0B' : '#F43F5E';

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', position: 'relative', padding: '8px 0 0' }}>
      <svg width="170" height="150" viewBox="0 0 170 150">
        <defs>
          <linearGradient id="gaugeGradTsx" x1="0%" y1="100%" x2="100%" y2="0%">
            <stop offset="0%" stopColor={gaugeColor} stopOpacity="0.8" />
            <stop offset="100%" stopColor={gaugeColor} stopOpacity="1" />
          </linearGradient>
        </defs>

        <circle
          cx={center}
          cy={center}
          r={radius}
          fill="none"
          stroke={isLight ? '#E2E8F0' : '#1E293B'}
          strokeWidth={strokeWidth}
          strokeDasharray={`${arcLength} ${circumference}`}
          strokeDashoffset="0"
          strokeLinecap="round"
          transform={`rotate(140 ${center} ${center})`}
        />

        <circle
          cx={center}
          cy={center}
          r={radius}
          fill="none"
          stroke="url(#gaugeGradTsx)"
          strokeWidth={strokeWidth}
          strokeDasharray={`${progressLength} ${circumference}`}
          strokeDashoffset="0"
          strokeLinecap="round"
          transform={`rotate(140 ${center} ${center})`}
          style={{ transition: 'stroke-dasharray 0.8s ease' }}
        />

        <text
          x={center}
          y={center - 2}
          textAnchor="middle"
          fill={isLight ? '#0F172A' : '#F8FAFC'}
          fontFamily="monospace"
          fontWeight="800"
          fontSize="24"
        >
          {availabilityPct.toFixed(1)}%
        </text>
        <text
          x={center}
          y={center + 18}
          textAnchor="middle"
          fill={isLight ? '#64748B' : '#94A3B8'}
          fontFamily="sans-serif"
          fontWeight="600"
          fontSize="10"
          letterSpacing="0.05em"
        >
          7-DAY UPTIME
        </text>
      </svg>

      <div style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        width: '100%',
        marginTop: '-6px',
        padding: '0 4px',
        fontSize: '11px',
        fontFamily: 'monospace',
        color: isLight ? '#475569' : '#94A3B8',
      }}>
        <span><strong>{gridHours7d.toFixed(1)}</strong> / 168.0 hrs</span>
        <span style={{
          color: gaugeColor,
          fontWeight: '700',
          padding: '2px 6px',
          backgroundColor: isLight ? (gaugeColor + '18') : (gaugeColor + '25'),
          borderRadius: '4px',
        }}>
          {(168.0 - gridHours7d).toFixed(1)}h outage
        </span>
      </div>
    </div>
  );
}

export default function SolarDashboard({
  initialMonth = 'all',
  autoFetchLive = true,
  dataset = RAW_ENERGY_DATASET,
  onMonthChange,
}: SolarDashboardProps) {
  const [liveData, setLiveData] = useState<DailyEnergyRecord[]>([]);
  const [selectedMonth, setSelectedMonth] = useState<string>(initialMonth);
  const [theme, setTheme] = useState<'dark' | 'light'>('dark');
  const [chart2Mode, setChart2Mode] = useState<'stacked' | 'grouped'>('stacked');
  const [visibleSeries, setVisibleSeries] = useState({ grid: true, solar: true, total: true });
  const [hoveredPointChart1, setHoveredPointChart1] = useState<DailyEnergyRecord | null>(null);
  const [hoveredPointChart2, setHoveredPointChart2] = useState<DailyEnergyRecord | null>(null);

  const isLight = theme === 'light';

  useEffect(() => {
    if (!autoFetchLive) return;
    const candidateBases = ['', 'https://localhost:7198', 'http://localhost:5031', 'http://127.0.0.1:5031'];

    (async () => {
      for (const base of candidateBases) {
        try {
          const res = await fetch(`${base}/api/solar/metrics/daily`, { cache: 'no-store' });
          if (!res.ok) continue;
          const json = await res.json();
          const list = Array.isArray(json) ? json : json.value || [];
          if (Array.isArray(list) && list.length > 0) {
            const mapped: DailyEnergyRecord[] = list.map((m: any) => {
              const [y, monthNum, d] = m.date.split('-').map(Number);
              const dt = new Date(y, monthNum - 1, d);
              const dayOfWeek = dt.toLocaleDateString('en-US', { weekday: 'short' });
              const monthName = dt.toLocaleDateString('en-US', { month: 'short' });
              const fullMonth = dt.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
              const formattedDate = `${dayOfWeek}, ${monthName} ${String(d).padStart(2, '0')}`;
              const shortDate = `${monthName} ${String(d).padStart(2, '0')}`;
              const dayLabel = `${dayOfWeek} ${d}`;
              const monthKey = `${y}-${String(monthNum).padStart(2, '0')}`;

              const gridConnectedHours = Math.round((m.gridUptimeHours || 0) * 10) / 10;
              const gridOutageHours = Math.round((m.gridOutageHours ?? Math.max(0, 24.0 - gridConnectedHours)) * 10) / 10;
              const totalTrackedHours = gridConnectedHours + gridOutageHours;
              const gridAvailabilityPercentage = totalTrackedHours > 0
                ? Math.round((gridConnectedHours / totalTrackedHours) * 1000) / 10
                : 0;

              const gridConsumptionKwh = Math.round((m.gridImportEnergyKwh || 0) * 100) / 100;
              const solarConsumptionKwh = Math.round((m.pvGenerationEnergyKwh || 0) * 100) / 100;
              const totalLoadKwh = Math.round((m.totalLoadEnergyKwh || 0) * 100) / 100;
              const totalConsumptionKwh = totalLoadKwh > 0 ? totalLoadKwh : Math.round((gridConsumptionKwh + solarConsumptionKwh) * 100) / 100;

              return {
                date: m.date,
                dayOfWeek,
                dayLabel,
                formattedDate,
                shortDate,
                monthKey,
                monthLabel: fullMonth,
                gridConnectedHours,
                gridOutageHours,
                gridAvailabilityPercentage,
                gridConsumptionKwh,
                solarConsumptionKwh,
                totalConsumptionKwh,
                solarGenerationKwh: solarConsumptionKwh,
                isRealData: true,
              };
            });
            setLiveData(mapped);
            break;
          }
        } catch (e) {
          // continue to next base
        }
      }
    })();
  }, [autoFetchLive]);

  const activeDataset = useMemo(() => {
    return liveData;
  }, [liveData]);

  const availableMonths = useMemo(() => {
    const map = new Map<string, string>();
    activeDataset.forEach((d) => {
      if (!map.has(d.monthKey)) {
        map.set(d.monthKey, d.monthLabel);
      }
    });
    const arr = Array.from(map.entries()).map(([k, label]) => {
      const count = activeDataset.filter((d) => d.monthKey === k).length;
      return { key: k, label: `${label} (${count} Days)` };
    });
    if (map.size > 1) {
      arr.push({ key: 'all', label: `All Recorded Months (${activeDataset.length} Days)` });
    }
    return arr;
  }, [activeDataset]);

  useEffect(() => {
    if (availableMonths.length > 0 && !availableMonths.some((m) => m.key === selectedMonth)) {
      setSelectedMonth('all');
    }
  }, [availableMonths, selectedMonth]);

  const handleMonthSelect = (month: string) => {
    setSelectedMonth(month);
    if (onMonthChange) {
      onMonthChange(month);
    }
  };

  const filteredData = useMemo(() => {
    if (selectedMonth === 'all') return activeDataset;
    return activeDataset.filter((d) => d.monthKey === selectedMonth);
  }, [activeDataset, selectedMonth]);

  const weeklyGaugeMetrics = useMemo<WeeklyGaugeMetrics>(() => {
    const last7Days = filteredData.slice(-7);
    const totalGridHours7d = last7Days.reduce((acc, d) => acc + d.gridConnectedHours, 0);
    const availabilityPct = (totalGridHours7d / 168.0) * 100;
    return {
      totalGridHours7d,
      availabilityPct: Math.round(availabilityPct * 10) / 10,
      totalOutageHours7d: Math.round((168.0 - totalGridHours7d) * 10) / 10,
    };
  }, [filteredData]);

  const monthlyOverview = useMemo<MonthlyOverviewMetrics>(() => {
    const totalGridKwh = filteredData.reduce((acc, d) => acc + d.gridConsumptionKwh, 0);
    const totalSolarKwh = filteredData.reduce((acc, d) => acc + d.solarConsumptionKwh, 0);
    const totalKwh = totalGridKwh + totalSolarKwh;
    return {
      totalGridKwh: Math.round(totalGridKwh * 10) / 10,
      totalSolarKwh: Math.round(totalSolarKwh * 10) / 10,
      totalKwh: Math.round(totalKwh * 10) / 10,
      avgDailyKwh: filteredData.length > 0 ? Math.round((totalKwh / filteredData.length) * 10) / 10 : 0,
      solarSharePct: totalKwh > 0 ? Math.round((totalSolarKwh / totalKwh) * 1000) / 10 : 0,
      gridSharePct: totalKwh > 0 ? Math.round((totalGridKwh / totalKwh) * 1000) / 10 : 0,
      daysCount: filteredData.length,
    };
  }, [filteredData]);

  const chart2MaxY = useMemo(() => {
    if (filteredData.length === 0) return 30;
    const maxVal = Math.max(...filteredData.map((d) => d.totalConsumptionKwh));
    return Math.max(10, Math.ceil((maxVal * 1.25) / 5) * 5);
  }, [filteredData]);

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      padding: '24px 20px 48px',
      gap: '24px',
      backgroundColor: isLight ? '#F1F5F9' : '#090E17',
      color: isLight ? '#0F172A' : '#F8FAFC',
      fontFamily: "'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, sans-serif",
    }}>
      {/* APP HEADER */}
      <header style={{
        width: '100%',
        maxWidth: '1320px',
        display: 'flex',
        flexWrap: 'wrap',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: '16px 22px',
        backgroundColor: isLight ? '#FFFFFF' : '#0F172A',
        border: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
        borderRadius: '14px',
        boxShadow: isLight ? '0 8px 24px rgba(0,0,0,0.05)' : '0 10px 25px rgba(0,0,0,0.4)',
        gap: '16px',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <div style={{
            width: '38px',
            height: '38px',
            borderRadius: '10px',
            background: 'linear-gradient(135deg, #0284C7 0%, #10B981 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#FFFFFF',
            fontWeight: '800',
            fontSize: '18px',
          }}>
            ⚡
          </div>
          <div>
            <h1 style={{ fontSize: '18px', fontWeight: '800', margin: 0, color: isLight ? '#0F172A' : '#F8FAFC' }}>
              SolarPulse | Energy Monitoring Dashboard
            </h1>
            <p style={{ fontSize: '12px', color: isLight ? '#64748B' : '#94A3B8', margin: '2px 0 0' }}>
              🟢 Live Inverter Database ({liveData.length} Days Recorded)
            </p>
          </div>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flexWrap: 'wrap' }}>
          {/* Month Filter */}
          <div style={{
            display: 'flex',
            alignItems: 'center',
            gap: '6px',
            backgroundColor: isLight ? '#F8FAFC' : '#162036',
            padding: '4px 8px',
            borderRadius: '8px',
            border: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
          }}>
            <span style={{ fontSize: '11px', fontWeight: '600', color: isLight ? '#64748B' : '#94A3B8' }}>Month:</span>
            <select
              value={selectedMonth}
              onChange={(e) => handleMonthSelect(e.target.value)}
              style={{
                backgroundColor: 'transparent',
                color: isLight ? '#0F172A' : '#F8FAFC',
                border: 'none',
                outline: 'none',
                fontWeight: '700',
                fontSize: '12px',
                cursor: 'pointer',
              }}
            >
              {availableMonths.map((m) => (
                <option key={m.key} value={m.key} style={{ backgroundColor: isLight ? '#FFFFFF' : '#0F172A' }}>
                  {m.label}
                </option>
              ))}
            </select>
          </div>

          <button
            onClick={() => setTheme(isLight ? 'dark' : 'light')}
            style={{
              padding: '7px 12px',
              fontSize: '12px',
              fontWeight: '700',
              color: isLight ? '#0F172A' : '#F8FAFC',
              backgroundColor: isLight ? '#F8FAFC' : '#162036',
              border: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
              borderRadius: '8px',
              cursor: 'pointer',
            }}
          >
            {isLight ? '🌙 Dark' : '☀️ Light'}
          </button>
        </div>
      </header>

      {/* SECTION 1: KPI CARDS & GAUGES */}
      <section style={{
        width: '100%',
        maxWidth: '1320px',
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(290px, 1fr))',
        gap: '16px',
      }}>
        {/* Weekly Grid Availability Gauge */}
        <div style={{
          backgroundColor: isLight ? '#FFFFFF' : '#0F172A',
          border: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
          borderRadius: '14px',
          padding: '18px 20px',
          boxShadow: isLight ? '0 4px 14px rgba(0,0,0,0.03)' : '0 4px 14px rgba(0,0,0,0.2)',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
        }}>
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ fontSize: '11px', fontWeight: '800', color: '#10B981', textTransform: 'uppercase' }}>Last 7 Days</span>
              <span style={{ fontSize: '10px', color: '#64748B' }}>Formula: (Hours/168) * 100%</span>
            </div>
            <h3 style={{ fontSize: '15px', fontWeight: '800', margin: '4px 0 0' }}>Weekly Grid Availability</h3>
          </div>

          <WeeklyAvailabilityGauge
            gridHours7d={weeklyGaugeMetrics.totalGridHours7d}
            availabilityPct={weeklyGaugeMetrics.availabilityPct}
            isLight={isLight}
          />
        </div>

        {/* Monthly Grid Consumption */}
        <div style={{
          backgroundColor: isLight ? '#FFFFFF' : '#0F172A',
          border: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
          borderRadius: '14px',
          padding: '18px 20px',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
        }}>
          <div>
            <span style={{ fontSize: '11px', fontWeight: '800', color: '#0284C7', textTransform: 'uppercase' }}>Grid Supply</span>
            <h3 style={{ fontSize: '14px', fontWeight: '700', color: '#94A3B8', margin: '2px 0 0' }}>Grid Consumption</h3>
            <div style={{ fontSize: '28px', fontWeight: '800', fontFamily: 'monospace', margin: '12px 0 4px' }}>
              {monthlyOverview.totalGridKwh.toLocaleString()} <span style={{ fontSize: '15px', color: '#64748B' }}>kWh</span>
            </div>
            <p style={{ fontSize: '12px', color: '#64748B' }}>{monthlyOverview.gridSharePct}% of total usage</p>
          </div>
          <div style={{ height: '6px', backgroundColor: isLight ? '#E2E8F0' : '#1E293B', borderRadius: '3px', overflow: 'hidden' }}>
            <div style={{ height: '100%', width: `${monthlyOverview.gridSharePct}%`, backgroundColor: '#0284C7' }} />
          </div>
        </div>

        {/* Monthly Solar Consumption */}
        <div style={{
          backgroundColor: isLight ? '#FFFFFF' : '#0F172A',
          border: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
          borderRadius: '14px',
          padding: '18px 20px',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
        }}>
          <div>
            <span style={{ fontSize: '11px', fontWeight: '800', color: '#F59E0B', textTransform: 'uppercase' }}>Solar Self-Use</span>
            <h3 style={{ fontSize: '14px', fontWeight: '700', color: '#94A3B8', margin: '2px 0 0' }}>Solar Consumption</h3>
            <div style={{ fontSize: '28px', fontWeight: '800', fontFamily: 'monospace', margin: '12px 0 4px' }}>
              {monthlyOverview.totalSolarKwh.toLocaleString()} <span style={{ fontSize: '15px', color: '#64748B' }}>kWh</span>
            </div>
            <p style={{ fontSize: '12px', color: '#64748B' }}>{monthlyOverview.solarSharePct}% solar self-sufficiency</p>
          </div>
          <div style={{ height: '6px', backgroundColor: isLight ? '#E2E8F0' : '#1E293B', borderRadius: '3px', overflow: 'hidden' }}>
            <div style={{ height: '100%', width: `${monthlyOverview.solarSharePct}%`, backgroundColor: '#F59E0B' }} />
          </div>
        </div>

        {/* Monthly Total Consumption */}
        <div style={{
          backgroundColor: isLight ? '#FFFFFF' : '#0F172A',
          border: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
          borderRadius: '14px',
          padding: '18px 20px',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
        }}>
          <div>
            <span style={{ fontSize: '11px', fontWeight: '800', color: '#8B5CF6', textTransform: 'uppercase' }}>Total = Grid + Solar</span>
            <h3 style={{ fontSize: '14px', fontWeight: '700', color: '#94A3B8', margin: '2px 0 0' }}>Total Consumption</h3>
            <div style={{ fontSize: '28px', fontWeight: '800', fontFamily: 'monospace', margin: '12px 0 4px' }}>
              {monthlyOverview.totalKwh.toLocaleString()} <span style={{ fontSize: '15px', color: '#64748B' }}>kWh</span>
            </div>
            <p style={{ fontSize: '12px', color: '#64748B' }}>Daily Average: {monthlyOverview.avgDailyKwh} kWh/day</p>
          </div>
          <div style={{
            padding: '6px 10px',
            backgroundColor: isLight ? '#F8FAFC' : '#162036',
            borderRadius: '6px',
            fontSize: '11px',
            fontFamily: 'monospace',
            color: '#8B5CF6',
          }}>
            {monthlyOverview.totalGridKwh} Grid + {monthlyOverview.totalSolarKwh} Solar
          </div>
        </div>
      </section>

      {/* SECTION 2: CHART 1 - DAILY GRID AVAILABILITY HOURS */}
      <section style={{
        width: '100%',
        maxWidth: '1320px',
        backgroundColor: isLight ? '#FFFFFF' : '#0F172A',
        border: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
        borderRadius: '14px',
        boxShadow: isLight ? '0 4px 14px rgba(0,0,0,0.03)' : '0 4px 14px rgba(0,0,0,0.2)',
        overflow: 'hidden',
      }}>
        <div style={{
          display: 'flex',
          flexWrap: 'wrap',
          justifyContent: 'space-between',
          alignItems: 'center',
          padding: '16px 22px',
          borderBottom: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
          gap: '12px',
        }}>
          <div>
            <h2 style={{ fontSize: '16px', fontWeight: '800', margin: 0 }}>
              Chart 1: Daily Grid Availability Hours
            </h2>
          </div>
        </div>

        {/* Readout subbar */}
        <div style={{
          minHeight: '38px',
          padding: '8px 22px',
          backgroundColor: isLight ? '#F8FAFC' : '#162036',
          borderBottom: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
          fontSize: '12px',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}>
          {hoveredPointChart1 ? (
            <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
              <span style={{ fontWeight: '800' }}>📅 {hoveredPointChart1.formattedDate}</span>
              <span>⚡ Connected: <strong style={{ color: '#10B981' }}>{hoveredPointChart1.gridConnectedHours}h</strong></span>
              <span>🔴 Outage: <strong style={{ color: '#F43F5E' }}>{hoveredPointChart1.gridOutageHours}h</strong></span>
              <span>Availability: <strong>{hoveredPointChart1.gridAvailabilityPercentage}%</strong></span>
            </div>
          ) : (
            <span style={{ color: '#64748B' }}>Hover over any day point or bar to inspect exact connected hours and outage duration.</span>
          )}
          <span style={{ fontSize: '11px', fontFamily: 'monospace', color: '#10B981' }}>Target: 24h</span>
        </div>

        {/* SVG Canvas with dynamic width so every day gets its explicit label */}
        <div style={{ padding: '24px 20px 20px', overflowX: 'auto' }}>
          {(() => {
            const count = filteredData.length;
            if (count === 0) return null;
            const svgWidth = Math.max(980, count * 44);
            const left = 60, right = svgWidth - 30, width = right - left;
            const step = count > 1 ? width / (count - 1) : 0;
            const getY = (h: number) => 195 - (h / 24) * 175;

            const points = filteredData.map((d, i) => ({
              x: count > 1 ? left + i * step : left + width / 2,
              y: getY(d.gridConnectedHours),
              d,
            }));

            const pathD = points.reduce((acc, p, i) => `${acc} ${i === 0 ? 'M' : 'L'} ${p.x},${p.y}`, '');

            return (
              <div style={{ width: `${svgWidth}px`, height: '260px' }}>
                <svg width={svgWidth} height="260" viewBox={`0 0 ${svgWidth} 260`}>
                  {[24, 18, 12, 6, 0].map((val) => {
                    const y = 195 - (val / 24) * 175;
                    return (
                      <g key={val}>
                        <line x1="50" y1={y} x2={svgWidth - 20} y2={y} stroke={val === 24 ? '#10B981' : (isLight ? '#E2E8F0' : '#1E293B')} strokeDasharray={val === 24 ? '4 4' : 'none'} />
                        <text x="42" y={y + 4} textAnchor="end" fill={val === 24 ? '#10B981' : '#64748B'} fontSize="10" fontFamily="monospace">{val}h</text>
                      </g>
                    );
                  })}

                  <g>
                      <path d={`${pathD} L ${points[points.length - 1].x},195 L ${points[0].x},195 Z`} fill="#10B981" fillOpacity="0.15" />
                      <path d={pathD} fill="none" stroke="#10B981" strokeWidth="2.8" strokeLinecap="round" strokeLinejoin="round" />
                      {points.map((pt) => {
                        const isHov = hoveredPointChart1?.date === pt.d.date;
                        return (
                          <g key={pt.d.date} onMouseEnter={() => setHoveredPointChart1(pt.d)} onMouseLeave={() => setHoveredPointChart1(null)} style={{ cursor: 'pointer' }}>
                            <line x1={pt.x} y1={pt.y} x2={pt.x} y2="195" stroke={isHov ? '#10B981' : '#64748B'} strokeWidth="1" strokeDasharray="2 2" opacity={isHov ? 0.8 : 0.3} />
                            <circle cx={pt.x} cy={pt.y} r="16" fill="transparent" />
                            <circle cx={pt.x} cy={pt.y} r={isHov ? 6.5 : 4} fill={isHov ? '#FFFFFF' : '#10B981'} stroke={isHov ? '#10B981' : '#FFFFFF'} strokeWidth="2" />
                            {/* EXACT VALUE LABEL ABOVE POINT */}
                            <text x={pt.x} y={Math.max(16, pt.y - 9)} textAnchor="middle" fill={isHov ? '#10B981' : (isLight ? '#334155' : '#94A3B8')} fontSize="10" fontFamily="monospace" fontWeight={isHov ? '800' : '600'}>
                              {pt.d.gridConnectedHours}h
                            </text>
                            {/* EXPLICIT DAY LABEL ON EVERY POINT */}
                            <text x={pt.x} y="218" textAnchor="middle" fill={isHov ? '#10B981' : (isLight ? '#0F172A' : '#F8FAFC')} fontSize="10" fontFamily="monospace" fontWeight="700">
                              {pt.d.dayLabel}
                            </text>
                            <text x={pt.x} y="232" textAnchor="middle" fill={isHov ? '#10B981' : '#64748B'} fontSize="9" fontFamily="monospace">
                              {pt.d.shortDate.split(' ')[0]}
                            </text>
                          </g>
                        );
                      })}
                    </g>
                </svg>
              </div>
            );
          })()}
        </div>
      </section>

      {/* SECTION 3: CHART 2 - DAILY CONSUMPTION BREAKDOWN & TOTAL */}
      <section style={{
        width: '100%',
        maxWidth: '1320px',
        backgroundColor: isLight ? '#FFFFFF' : '#0F172A',
        border: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
        borderRadius: '14px',
        boxShadow: isLight ? '0 4px 14px rgba(0,0,0,0.03)' : '0 4px 14px rgba(0,0,0,0.2)',
        overflow: 'hidden',
      }}>
        <div style={{
          display: 'flex',
          flexWrap: 'wrap',
          justifyContent: 'space-between',
          alignItems: 'center',
          padding: '16px 22px',
          borderBottom: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
          gap: '12px',
        }}>
          <div>
            <h2 style={{ fontSize: '16px', fontWeight: '800', margin: 0, display: 'flex', alignItems: 'center', gap: '8px' }}>
              Chart 2: Daily Consumption Breakdown & Total
              <span style={{ fontSize: '11px', fontWeight: '700', color: '#8B5CF6', backgroundColor: 'rgba(139, 92, 246, 0.12)', padding: '2px 8px', borderRadius: '4px' }}>
                Grid + Solar + Total Overlay
              </span>
            </h2>
            <span style={{ fontSize: '11px', color: isLight ? '#64748B' : '#94A3B8' }}>
              X-axis: Day & Date • Y-axis: Energy in kilowatt-hours (kWh)
            </span>
          </div>

          <div style={{ display: 'flex', gap: '8px', alignItems: 'center', flexWrap: 'wrap' }}>
            <div style={{ display: 'flex', gap: '6px', fontSize: '11px' }}>
              <button
                onClick={() => setVisibleSeries((p) => ({ ...p, grid: !p.grid }))}
                style={{
                  padding: '4px 8px',
                  borderRadius: '5px',
                  backgroundColor: visibleSeries.grid ? '#0284C7' : 'transparent',
                  color: visibleSeries.grid ? '#FFFFFF' : '#64748B',
                  border: '1px solid #0284C7',
                  cursor: 'pointer',
                }}
              >
                Grid (kWh)
              </button>
              <button
                onClick={() => setVisibleSeries((p) => ({ ...p, solar: !p.solar }))}
                style={{
                  padding: '4px 8px',
                  borderRadius: '5px',
                  backgroundColor: visibleSeries.solar ? '#F59E0B' : 'transparent',
                  color: visibleSeries.solar ? '#FFFFFF' : '#64748B',
                  border: '1px solid #F59E0B',
                  cursor: 'pointer',
                }}
              >
                Solar (kWh)
              </button>
              <button
                onClick={() => setVisibleSeries((p) => ({ ...p, total: !p.total }))}
                style={{
                  padding: '4px 8px',
                  borderRadius: '5px',
                  backgroundColor: visibleSeries.total ? '#8B5CF6' : 'transparent',
                  color: visibleSeries.total ? '#FFFFFF' : '#64748B',
                  border: '1px solid #8B5CF6',
                  cursor: 'pointer',
                }}
              >
                Total Line
              </button>
            </div>

            <div style={{ display: 'flex', gap: '4px' }}>
              <button
                onClick={() => setChart2Mode('stacked')}
                style={{
                  padding: '4px 10px',
                  fontSize: '11px',
                  fontWeight: chart2Mode === 'stacked' ? '700' : '500',
                  backgroundColor: chart2Mode === 'stacked' ? '#8B5CF6' : 'transparent',
                  color: chart2Mode === 'stacked' ? '#FFFFFF' : (isLight ? '#475569' : '#94A3B8'),
                  border: `1px solid ${chart2Mode === 'stacked' ? '#8B5CF6' : (isLight ? '#CBD5E1' : '#334155')}`,
                  borderRadius: '6px',
                  cursor: 'pointer',
                }}
              >
                Stacked
              </button>
              <button
                onClick={() => setChart2Mode('grouped')}
                style={{
                  padding: '4px 10px',
                  fontSize: '11px',
                  fontWeight: chart2Mode === 'grouped' ? '700' : '500',
                  backgroundColor: chart2Mode === 'grouped' ? '#8B5CF6' : 'transparent',
                  color: chart2Mode === 'grouped' ? '#FFFFFF' : (isLight ? '#475569' : '#94A3B8'),
                  border: `1px solid ${chart2Mode === 'grouped' ? '#8B5CF6' : (isLight ? '#CBD5E1' : '#334155')}`,
                  borderRadius: '6px',
                  cursor: 'pointer',
                }}
              >
                Grouped
              </button>
            </div>
          </div>
        </div>

        <div style={{
          minHeight: '38px',
          padding: '8px 22px',
          backgroundColor: isLight ? '#F8FAFC' : '#162036',
          borderBottom: `1px solid ${isLight ? '#E2E8F0' : '#1E293B'}`,
          fontSize: '12px',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}>
          {hoveredPointChart2 ? (
            <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
              <span style={{ fontWeight: '800' }}>📅 {hoveredPointChart2.formattedDate}</span>
              <span>⚡ Grid: <strong style={{ color: '#0284C7' }}>{hoveredPointChart2.gridConsumptionKwh} kWh</strong></span>
              <span>☀️ Solar: <strong style={{ color: '#F59E0B' }}>{hoveredPointChart2.solarConsumptionKwh} kWh</strong></span>
              <span>Total: <strong style={{ color: '#8B5CF6' }}>{hoveredPointChart2.totalConsumptionKwh} kWh</strong></span>
            </div>
          ) : (
            <span style={{ color: '#64748B' }}>Hover over any day bar or total node to inspect daily energy breakdown.</span>
          )}
          <span style={{ fontSize: '11px', fontFamily: 'monospace', color: '#64748B' }}>Max: {chart2MaxY} kWh</span>
        </div>

        <div style={{ padding: '24px 20px 20px', overflowX: 'auto' }}>
          {(() => {
            const count = filteredData.length;
            if (count === 0) return null;
            const svgWidth = Math.max(980, count * 44);
            const left = 60, right = svgWidth - 30, width = right - left;
            const step = count > 1 ? width / (count - 1) : 0;
            const barW = Math.max(8, Math.min(26, (width / count) * 0.65));
            const getY = (kwh: number) => 205 - (kwh / chart2MaxY) * 185;

            const linePoints = filteredData.map((d, i) => ({
              x: count > 1 ? left + i * step : left + width / 2,
              y: getY(d.totalConsumptionKwh),
              d,
            }));

            const linePath = linePoints.reduce((acc, p, i) => `${acc} ${i === 0 ? 'M' : 'L'} ${p.x},${p.y}`, '');

            return (
              <div style={{ width: `${svgWidth}px`, height: '270px' }}>
                <svg width={svgWidth} height="270" viewBox={`0 0 ${svgWidth} 270`}>
                  {[0, 0.25, 0.5, 0.75, 1.0].map((frac) => {
                    const yVal = Math.round(chart2MaxY * (1 - frac));
                    const yPos = 20 + frac * 185;
                    return (
                      <g key={frac}>
                        <line x1="50" y1={yPos} x2={svgWidth - 20} y2={yPos} stroke={isLight ? '#E2E8F0' : '#1E293B'} strokeWidth="1" />
                        <text x="42" y={yPos + 4} textAnchor="end" fill="#64748B" fontFamily="monospace" fontSize="10">{yVal}</text>
                      </g>
                    );
                  })}

                  {/* Bars */}
                  {filteredData.map((d, i) => {
                    const cx = count > 1 ? left + i * step : left + width / 2;
                    const isHov = hoveredPointChart2?.date === d.date;

                    if (chart2Mode === 'stacked') {
                      const solarH = (d.solarConsumptionKwh / chart2MaxY) * 185;
                      const gridH = (d.gridConsumptionKwh / chart2MaxY) * 185;
                      const solarY = 205 - solarH;
                      const gridY = solarY - gridH;
                      const bx = cx - barW / 2;

                      return (
                        <g key={d.date} onMouseEnter={() => setHoveredPointChart2(d)} onMouseLeave={() => setHoveredPointChart2(null)} style={{ cursor: 'pointer' }}>
                          {visibleSeries.solar && <rect x={bx} y={solarY} width={barW} height={solarH} fill="#F59E0B" rx="2" />}
                          {visibleSeries.grid && <rect x={bx} y={gridY} width={barW} height={gridH} fill="#0284C7" rx="2" />}
                          <text x={cx} y="228" textAnchor="middle" fill={isHov ? '#8B5CF6' : (isLight ? '#0F172A' : '#F8FAFC')} fontSize="10" fontFamily="monospace" fontWeight="700">
                            {d.dayLabel}
                          </text>
                          <text x={cx} y="242" textAnchor="middle" fill={isHov ? '#8B5CF6' : '#64748B'} fontSize="9" fontFamily="monospace">
                            {d.shortDate.split(' ')[0]}
                          </text>
                        </g>
                      );
                    } else {
                      const subW = barW / 2 - 1;
                      const gridH = (d.gridConsumptionKwh / chart2MaxY) * 185;
                      const solarH = (d.solarConsumptionKwh / chart2MaxY) * 185;
                      const gx = cx - subW - 1;
                      const sx = cx + 1;
                      return (
                        <g key={d.date} onMouseEnter={() => setHoveredPointChart2(d)} onMouseLeave={() => setHoveredPointChart2(null)} style={{ cursor: 'pointer' }}>
                          {visibleSeries.grid && <rect x={gx} y={205 - gridH} width={subW} height={gridH} fill="#0284C7" rx="2" />}
                          {visibleSeries.solar && <rect x={sx} y={205 - solarH} width={subW} height={solarH} fill="#F59E0B" rx="2" />}
                          <text x={cx} y="228" textAnchor="middle" fill={isHov ? '#8B5CF6' : (isLight ? '#0F172A' : '#F8FAFC')} fontSize="10" fontFamily="monospace" fontWeight="700">
                            {d.dayLabel}
                          </text>
                          <text x={cx} y="242" textAnchor="middle" fill={isHov ? '#8B5CF6' : '#64748B'} fontSize="9" fontFamily="monospace">
                            {d.shortDate.split(' ')[0]}
                          </text>
                        </g>
                      );
                    }
                  })}

                  {/* Total Line Overlay */}
                  {visibleSeries.total && (
                    <g>
                      <path d={linePath} fill="none" stroke="#8B5CF6" strokeWidth="2.8" strokeLinecap="round" strokeLinejoin="round" />
                      {linePoints.map(pt => (
                        <circle key={pt.d.date} cx={pt.x} cy={pt.y} r={hoveredPointChart2?.date === pt.d.date ? 5.5 : 3} fill="#8B5CF6" stroke="#FFFFFF" strokeWidth="1.5" />
                      ))}
                    </g>
                  )}
                </svg>
              </div>
            );
          })()}
        </div>
      </section>
    </div>
  );
}
