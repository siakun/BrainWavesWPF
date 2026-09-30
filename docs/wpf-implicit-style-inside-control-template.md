# 테마 라이브러리의 암묵 스타일이 컨트롤 템플릿 안으로 들어오는 문제

직접 만든 ControlTemplate 안의 컨트롤이 템플릿에 적지 않은 크기나 정렬로 그려질 때 보는 문서입니다. 앱 리소스에 병합한 암묵 스타일(`x:Key` 없이 `TargetType`만 적은 스타일)은 창에 놓은 컨트롤뿐 아니라 템플릿 안에 선언한 컨트롤에도 붙습니다. 템플릿에서 `Template`만 바꾸면 모양은 바뀌어도 그 스타일의 다른 Setter는 남습니다. Material Design in XAML처럼 기본 컨트롤 전체에 암묵 스타일을 거는 테마 라이브러리를 쓰면서 컨트롤 템플릿을 직접 그리는 경우에 해당합니다.

## 증상

닫힌 콤보 상자를 버튼과 같은 알약 모양으로 그리려고 ComboBox 템플릿을 직접 작성했습니다. 템플릿은 전체를 덮는 `ToggleButton` 위에 선택된 항목과 화살표를 올리는 흔한 구조입니다.

```xml
<ControlTemplate TargetType="ComboBox">
    <Grid>
        <ToggleButton IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}">
            <ToggleButton.Template>
                <ControlTemplate TargetType="ToggleButton">
                    <Border CornerRadius="17" BorderThickness="1" BorderBrush="...">
                        <!-- 화살표 아이콘 -->
                    </Border>
                </ControlTemplate>
            </ToggleButton.Template>
        </ToggleButton>
        <ContentPresenter Content="{TemplateBinding SelectionBoxItem}" ... />
        <Popup ... />
    </Grid>
</ControlTemplate>
```

템플릿 어디에도 폭이나 정렬을 적지 않았는데, 테두리가 콤보 상자 전체가 아니라 가운데의 작은 알약으로 줄어 화살표만 감쌌습니다. 선택된 항목의 글자는 제자리에 그려졌으므로 Grid의 열 배치 문제는 아니었습니다.

앱 리소스에는 Material Design 3 기본 사전(`MaterialDesign3.Defaults.xaml`)이 병합되어 있었습니다. 이 사전은 `ToggleButton`에 켜고 끄는 스위치 모양의 스타일을 암묵 스타일로 겁니다.

겉보기가 비슷하지만 원인이 다른 경우도 있습니다. 부모 Grid의 열이 `Auto`이거나 상위 컨트롤의 `HorizontalContentAlignment`가 `Stretch`가 아니면 같은 모양이 나옵니다. 이런 경우는 아래 진단의 `Style="{x:Null}"` 비교에서 모양이 바뀌지 않으므로 구별됩니다.

## 원인

WPF는 `Style`에 값을 주지 않은 요소에 대해, 그 요소의 타입을 키로 리소스를 찾아 암묵 스타일을 붙입니다. Application 리소스와 그 병합 사전에 있는 암묵 스타일은 앱 어디에서나 조회되므로, ControlTemplate 안에 선언한 `ToggleButton`도 창에 놓은 `ToggleButton`과 똑같이 스위치 스타일을 받았습니다.

템플릿 안에서 `ToggleButton.Template`을 지정하면 스타일의 Template Setter만 이깁니다. 로컬 값이 스타일 값보다 우선하기 때문입니다. 스타일의 나머지 Setter는 그대로 적용됩니다. 앱 리소스에서 `ToggleButton`의 암묵 스타일을 찾아 `BasedOn` 사슬의 Setter를 읽어 보니 스위치 크기인 `Width = 52`, `Height = 32`가 들어 있었습니다. 폭이 고정된 요소는 `HorizontalAlignment`가 `Stretch`여도 늘어나지 않고 남은 공간의 가운데에 놓이므로, 테두리가 52픽셀짜리 알약으로 줄어 가운데에 그려졌습니다.

## 진단 절차

1. 모양이 어긋난 요소의 타입을 찾습니다. 템플릿 안의 요소라면 그 타입에 앱 수준 암묵 스타일이 있는지부터 의심합니다. 테마 라이브러리를 쓰면 대개 있습니다.
2. 그 요소에 `Style="{x:Null}"`을 임시로 주고 다시 그립니다. `Style`에 로컬 값(null)이 있으면 암묵 스타일을 찾지 않습니다.
   - 모양이 바로잡히면 암묵 스타일이 원인입니다. 해결 방법에서 대응을 고릅니다.
   - 그대로면 암묵 스타일은 원인이 아닙니다. 부모 레이아웃의 열과 정렬, 템플릿 바인딩을 봅니다.
3. 어느 Setter가 문제인지 알아야 하면 앱 리소스에서 그 타입의 암묵 스타일을 찾아 `BasedOn` 사슬을 따라 Setter를 출력합니다. 테마 라이브러리의 암묵 스타일은 대개 `BasedOn`으로 이름 있는 스타일을 이어받으므로 사슬 끝까지 봐야 합니다. Visual Studio의 Live Property Explorer 같은 도구로 속성 값의 출처를 봐도 됩니다.

```csharp
var style = Application.Current.TryFindResource(typeof(ToggleButton)) as Style;
for (int depth = 0; style != null; depth++, style = style.BasedOn)
    foreach (var setter in style.Setters.OfType<Setter>())
        Debug.WriteLine($"[{depth}] {setter.Property.Name} = {setter.Value}");
```

이 저장소에서는 첫 단계(`[0]`)에는 Setter가 없고, `BasedOn`으로 이어받은 스위치 스타일(`[1]`)에 `Width`와 `Height`가 있었습니다.

## 해결 방법과 선택 기준

| 상황 | 대응 | 이유 |
|---|---|---|
| 템플릿의 부품으로만 쓰는 컨트롤 | `Style="{x:Null}"`에 `Template`을 직접 지정 | 부품은 템플릿이 모양을 전부 정하므로 암묵 스타일에서 가져올 것이 없습니다 |
| 같은 부품을 여러 템플릿에서 씀 | `x:Key` 스타일을 만들어 명시적으로 지정 (`BasedOn` 없이) | 명시적 스타일이 있으면 암묵 스타일을 찾지 않고, 모양을 한 곳에서 고칩니다 |
| 암묵 스타일의 일부(포커스 표시 등)는 유지하고 싶음 | `BasedOn`으로 이어받고 문제 속성만 다시 지정 | 필요한 Setter를 살리되 라이브러리 버전이 바뀌면 다시 확인해야 합니다 |
| 앱 전체에서 그 타입의 모양을 바꾸려 함 | 같은 타입의 암묵 스타일을 테마 사전보다 뒤에 병합한 사전에 정의 | 같은 키가 여러 병합 사전에 있으면 나중에 병합한 사전이 이깁니다 |

이 저장소에서는 콤보 상자 내부의 `ToggleButton`에 첫째 대응을 썼습니다. 같은 앱에서 Slider와 ScrollBar는 넷째 대응으로 암묵 스타일을 다시 정의했고, 테마 사전보다 뒤에 병합해 앱의 모든 슬라이더와 스크롤 막대가 새 모양으로 바뀐 것을 화면에서 확인했습니다. Slider와 ScrollBar의 템플릿 안에 쓰는 `RepeatButton`과 `Thumb`은 처음부터 `x:Key` 스타일을 명시해 이 문제가 생기지 않았습니다.

`Style="{x:Null}"`은 암묵 스타일만 끊습니다. 대상 타입의 기본 테마 스타일(`DefaultStyleKey`로 찾는 스타일)은 별개라서 계속 적용되지만, 템플릿을 직접 지정했다면 모양에는 영향이 없습니다.

## 검증과 회귀 확인

- 고치기 전후의 화면을 같은 조건에서 비교합니다. 이 저장소에서는 버전 목록을 채운 설정 화면을 그려, 테두리가 화살표 주변의 작은 알약에서 콤보 상자 전체 폭의 알약으로 바뀌고 드롭다운 목록도 그 아래에 열리는 것을 확인했습니다.
- 테마 라이브러리를 올리면 암묵 스타일의 Setter가 바뀔 수 있습니다. 직접 그린 템플릿이 있는 화면은 라이브러리 버전을 올린 뒤 다시 그려 봅니다.

## 근거

- 적용한 코드: [ComboBoxStyles.xaml](../BrainWaves/BrainWaves/Resources/Styles/ComboBoxStyles.xaml)
- 앱 전체 암묵 스타일을 다시 정의한 예: [SliderStyles.xaml](../BrainWaves/BrainWaves/Resources/Styles/SliderStyles.xaml)
- 암묵 스타일과 병합 순서: [Styles and templates overview (WPF .NET)](https://learn.microsoft.com/dotnet/desktop/wpf/controls/styles-templates-overview), [Merged resource dictionaries](https://learn.microsoft.com/dotnet/desktop/wpf/systems/xaml-resources-merged-dictionaries)
