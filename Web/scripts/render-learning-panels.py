"""Original typographic learning diagrams; no third-party photographs or assets.
Run with Python + Pillow. Font can be supplied as PALABRAVO_PANEL_FONT.
"""
from pathlib import Path
import os
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
FONT=os.environ.get('PALABRAVO_PANEL_FONT','/System/Library/Fonts/Supplemental/Arial.ttf')
def font(n): return ImageFont.truetype(FONT,n)
def panel(title):
 im=Image.new('RGB',(1000,760),'#fff8ed');d=ImageDraw.Draw(im);d.rounded_rectangle((15,15,985,745),radius=24,outline='#294754',width=5);d.text((55,45),title,font=font(46),fill='#173744');return im,d
def save(im,name):
 for folder in [ROOT/'Web/public/content',ROOT/'Palabravo/Resources/Images']:
  folder.mkdir(parents=True,exist_ok=True);im.save(folder/name)
im,d=panel('MERCADO · PRECIOS POR KILO')
d.text((70,130),'Producto',font=font(38),fill='#173744');d.text((650,130),'Precio',font=font(38),fill='#173744')
for i,(a,b) in enumerate([('Manzanas','4'),('Peras','3'),('Naranjas','2'),('Uvas','5')]):
 y=210+i*100;d.rounded_rectangle((55,y-10,940,y+70),12,fill='#e6efdf' if i%2==0 else '#f3e5d2');d.text((80,y),a,font=font(40),fill='#173744');d.text((680,y),b,font=font(40),fill='#173744')
d.text((70,665),'Precios en unidades monetarias.',font=font(29),fill='#173744');save(im,'market_board.png')
im,d=panel('ESTACIÓN · HORARIO DE HOY')
headers=['Línea','Salida','Llegada','Destino','Cambios'];xs=[45,205,370,560,800]
for x,t in zip(xs,headers):d.text((x,145),t,font=font(29),fill='#173744')
for i,row in enumerate([['A','09:10','09:30','Museo','0'],['B','09:25','09:45','Museo','0'],['C','09:30','10:00','Parque','1']]):
 y=245+i*115;d.rounded_rectangle((35,y-10,955,y+75),10,fill='#e6efdf' if i%2==0 else '#f3e5d2')
 for x,t in zip(xs,row):d.text((x,y+5),t,font=font(32),fill='#173744')
d.text((55,645),'Cada fila representa un trayecto.',font=font(31),fill='#173744');save(im,'station_board.png')
im,d=panel('PLANO · DOS RUTAS AL MUSEO')
pts={'Inicio':(140,390),'Biblioteca':(400,230),'Parque':(760,230),'Museo':(860,520),'Escalera':(400,570)}
for a,b in [('Inicio','Biblioteca'),('Biblioteca','Parque'),('Parque','Museo')]:d.line([pts[a],pts[b]],fill='#34825a',width=12)
for a,b in [('Inicio','Escalera'),('Escalera','Museo')]:d.line([pts[a],pts[b]],fill='#be553d',width=12)
for name,(x,y) in pts.items():
 d.ellipse((x-14,y-14,x+14,y+14),fill='#173744');w=d.textlength(name,font=font(31));d.rectangle((x-w/2-6,y-67,x+w/2+6,y-22),fill='#fff8ed');d.text((x-w/2,y-64),name,font=font(31),fill='#173744')
d.text((60,670),'Verde: accesible · Roja: con escalera',font=font(30),fill='#173744');save(im,'route_board.png')
