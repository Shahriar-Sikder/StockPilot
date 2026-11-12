/**
 * The mockup's skel(): a heading bar, four KPI blocks and seven table lines, shown in the page area
 * while a page loads its data. `kpis={false}` drops the KPI row for list pages.
 */
export default function Skeleton({ kpis = true, lines = 7 }) {
  return <>
    <div className="sk" style={{ height: 26, width: 200, marginBottom: 14 }} />
    {kpis && <div className="kp">{[0, 1, 2, 3].map((i) => <div key={i}><div className="sk" style={{ height: 12, width: 90, marginBottom: 10 }} /><div className="sk" style={{ height: 22, width: 120 }} /></div>)}</div>}
    <div className="card">{Array.from({ length: lines }, (_, i) => <div key={i} className="sk" style={{ height: 14, margin: '12px 0' }} />)}</div>
  </>;
}
